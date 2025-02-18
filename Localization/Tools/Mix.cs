using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Localization.Tools;
public static class Mix
{
    //将目录打包成mix
    public static void PackToMix(string path, string MixName)
    {
        string command = $" pack -game ra2 -mix \"{MixName}\" -dir \"{path}\" -database \"Resources\\global mix database.dat\"";

        Process process = new Process();
        process.StartInfo.FileName = "Resources\\ccmixar.exe";
        process.StartInfo.Arguments = command;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        process.WaitForExit();
      
    }

    /// <summary>
    /// 打包文件列表到指定的MIX文件中。
    /// </summary>
    /// <param name="filePaths">要打包的文件列表。</param>
    /// <param name="outputDirectory">输出MIX文件的目录。</param>
    /// <param name="mixName">生成的MIX文件名。</param>
    public static void PackFilesToMix(List<string> filePaths, string outputDirectory, string mixName)
    {
        // 创建临时目录来存放文件
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            // 将文件复制到临时目录
            foreach (var filePath in filePaths)
            {
                string fileName = Path.GetFileName(filePath);
                File.Copy(filePath, Path.Combine(tempDir, fileName));
            }

            // 执行打包操作
            string mixPath = Path.Combine(outputDirectory, mixName);
            PackToMix(tempDir, mixPath);
        }
        finally
        {
            // 清理临时目录
            Directory.Delete(tempDir, true);
        }
    }

    public static void UnPackMix(string path, string MixName,bool del = false)
    {
        string command = $" unpack -game ra2 -mix \"{MixName}\" -dir \"Resources/MissionCashe\"";

        Console.WriteLine(command);

        Process process = new();
        process.StartInfo.FileName = "Resources\\ccmixar.exe";
        process.StartInfo.Arguments = command;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        process.Start();

        process.WaitForExit();

        if (!Directory.Exists("Resources/MissionCashe/"))
            Directory.CreateDirectory("Resources/MissionCashe/");
        if(!Directory.Exists(path))
            Directory.CreateDirectory(path);

        foreach (string file in Directory.GetFiles("Resources/MissionCashe/"))
            File.Move(file, Path.Combine(path,Path.GetFileName(file)), true);
        if(del)
            File.Delete(MixName);
        Directory.Delete("Resources/MissionCashe/", true);
         
    }

}
