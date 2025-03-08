using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ZIP;

public class SevenZip
{

    public delegate void ProgressCallback(double progressPercentage);

    public static void ExtractWith7Zip(string archivePath, string extractPath, ProgressCallback progressCallback = null,bool needDel = false)
    {
        try
        {
            // 构造命令行参数
            string arguments = $"x -y -aoa \"{archivePath}\" -o\"{extractPath}\"";

            string architecture = Environment.Is64BitProcess ? "x64" : "x86";
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm || RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                architecture = "arm64";
            }

            // 启动 7z.exe 进程
            ProcessStartInfo startInfo = new()
            {
                FileName = $"Resources/Binaries/{architecture}/7z.exe",
                Arguments = arguments,
                CreateNoWindow = true, // 不显示命令行窗口
                UseShellExecute = false, // 不使用操作系统外壳程序启动进程
                RedirectStandardOutput = true // 重定向标准输出流
            };

            Console.WriteLine(startInfo.FileName);
            Console.WriteLine(arguments);

         

            using Process process = new();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // 解析输出流中的进度信息
                    if (int.TryParse(e.Data, out int progress))
                    {
                        progressCallback?.Invoke(progress);
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine(); // 开始异步读取标准输出流

            process.WaitForExit(); // 等待解压完成

            if (needDel)
            {
                try
                {
                    File.Delete(archivePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file {archivePath}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
          
            Console.WriteLine($"Error extracting archive {archivePath} to {extractPath} with 7z.exe: {ex.Message}");
        }
    }

   

}