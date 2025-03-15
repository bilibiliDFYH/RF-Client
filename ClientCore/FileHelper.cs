using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore
{
    public static class FileHelper
    {
        public static void CopyFile(string sourceFilePath, string saveFilePath, bool killProcesses = false)
        {
            if (File.Exists(sourceFilePath))
            {
                // 如果需要解除进程占用，先调用 KillProcessUsingFile
                if (killProcesses)
                {
                    KillProcessUsingFile(sourceFilePath);
                }

                // 复制文件
                try
                {
                    File.Copy(sourceFilePath, saveFilePath, true);
                    Console.WriteLine($"File copied successfully from {sourceFilePath} to {saveFilePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error copying file: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Source file does not exist: {sourceFilePath}");
            }
        }

        public static void CopyDirectory(string sourceDirPath, string saveDirPath, List<string> ignoreExtensions = null, bool killProcesses = false)
        {
            // 如果未传入 ignoreExtensions，则默认为空列表
            ignoreExtensions ??= new List<string>();

            if (!string.IsNullOrEmpty(sourceDirPath) && Directory.Exists(sourceDirPath))
            {
                if (!Directory.Exists(saveDirPath))
                {
                    Directory.CreateDirectory(saveDirPath);
                }
                else
                {
                    // 解除目标文件夹内所有文件的只读属性
                    foreach (string file in Directory.GetFiles(saveDirPath, "*", SearchOption.AllDirectories))
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                    }
                }

                // 复制文件
                string[] files = Directory.GetFiles(sourceDirPath);
                foreach (string file in files)
                {
                    // 获取文件的扩展名
                    string fileExtension = Path.GetExtension(file).ToLower();

                    // 如果当前文件的扩展名在忽略列表中，则跳过该文件
                    if (ignoreExtensions.Any(ext => fileExtension.EndsWith(ext.ToLower())))
                    {
                        continue;
                    }

                    string pFilePath = Path.Combine(saveDirPath, Path.GetFileName(file));

                    // 如果需要解除进程占用，先调用 KillProcessUsingFile
                    if (killProcesses)
                    {
                        KillProcessUsingFile(file);
                    }

                    // 复制文件
                    File.Copy(file, pFilePath, true);
                }

                // 递归复制子文件夹
                string[] directories = Directory.GetDirectories(sourceDirPath);
                foreach (string directory in directories)
                {
                    string pDirPath = Path.Combine(saveDirPath, Path.GetFileName(directory));
                    CopyDirectory(directory, pDirPath, ignoreExtensions, killProcesses);
                }
            }
        }

        public static void ForceDeleteDirectory(string targetDir)
        {
            if (!Directory.Exists(targetDir))
                return;

            // 移除文件的只读属性并删除文件
            foreach (string file in Directory.GetFiles(targetDir))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            // 递归处理子目录
            foreach (string dir in Directory.GetDirectories(targetDir))
            {
                ForceDeleteDirectory(dir);
            }

            // 移除目录的只读属性并删除空目录
            try
            {
                Directory.Delete(targetDir, false);
            }
            catch (IOException)
            {
                // 处理可能的异常，如目录非空（理论上不应发生）
                File.SetAttributes(targetDir, FileAttributes.Normal);
                Directory.Delete(targetDir, false);
            }
            catch (UnauthorizedAccessException)
            {
                File.SetAttributes(targetDir, FileAttributes.Normal);
                Directory.Delete(targetDir, false);
            }
        }

        public static void ForceMoveFile(string sourceFile, string destFile)
        {
            if (File.Exists(destFile))
            {
                File.SetAttributes(destFile, FileAttributes.Normal);
                File.Delete(destFile);
            }
            File.Move(sourceFile, destFile,true);
        }

        public static void ReNameCustomFile(bool Online = false)
        {
            List<string> customFile = [
                //"custom_art_all.ini", 
               // "custom_art_ra2.ini",
                //"custom_art_yr.ini",
                "custom_rules_all.ini",
                //"custom_rules_ra2.ini",
               // "custom_rules_yr.ini"
                ];

            var tag = Online ? string.Empty : "Online";
            var tag2 = Online ? "Online" :  string.Empty;

            foreach (var fileName in customFile)
            {
                var ini = $"{ProgramConstants.GamePath}Client/{tag}{fileName}";

                if (File.Exists(ini))
                {
                    File.Move(ini, $"{ProgramConstants.GamePath}Client/{tag2}{fileName}",true);
                }
            }
        }

        /// <summary>
        /// 查找并杀死占用该文件的进程
        /// </summary>
        private static void KillProcessUsingFile(string filePath)
        {
            try
            {
                // 使用 cmd 的 tasklist 命令查找占用文件的进程
                string taskKillCommand = $"tasklist /fi \"imagename eq {filePath}\"";

                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c {taskKillCommand}";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // 输出结果并处理，找出占用该文件的进程ID
                if (output.Contains(filePath))
                {
                    string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line.Contains(filePath))
                        {
                            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            string pid = parts[1]; // 提取 PID
                            KillProcessById(pid);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 通过 PID 杀死进程
        /// </summary>
        private static void KillProcessById(string pid)
        {
            try
            {
                Process killProcess = new Process();
                killProcess.StartInfo.FileName = "cmd.exe";
                killProcess.StartInfo.Arguments = $"/c taskkill /pid {pid} /f"; // /f 强制杀死进程
                killProcess.StartInfo.UseShellExecute = false;
                killProcess.StartInfo.CreateNoWindow = true;
                killProcess.Start();
                killProcess.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error killing process: {ex.Message}");
            }
        }
    }
}


