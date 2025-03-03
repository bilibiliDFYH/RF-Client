using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore
{
    public static class FileHelper
    {
        public static void CopyDirectory(string sourceDirPath, string saveDirPath, List<string> ignoreExtensions = null)
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
                    File.Copy(file, pFilePath, true);
                }

                // 递归复制子文件夹
                string[] directories = Directory.GetDirectories(sourceDirPath);
                foreach (string directory in directories)
                {
                    string pDirPath = Path.Combine(saveDirPath, Path.GetFileName(directory));
                    CopyDirectory(directory, pDirPath, ignoreExtensions);
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
                "custom_art_all.ini", 
                "custom_art_ra2.ini",
                "custom_art_yr.ini",
                "custom_rules_all.ini",
                "custom_rules_ra2.ini",
                "custom_rules_yr.ini"
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
    }
}


