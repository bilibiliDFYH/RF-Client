namespace ClientUpdater;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Fun;

internal sealed class Program
{
    private static ConsoleColor defaultColor;
    private static StreamWriter errorWriter;

    /// <summary>
    /// 更新程序 For Ra2Client Reunion
    /// </summary>
    /// <param name="args">可执行程序路径 根目录</param>
    private static void Main(string[] args)
    {
        foreach (var arg in args)
        {
            Write(arg);
        }

        defaultColor = Console.ForegroundColor;

        var errorLogPath = "Client/update_log.txt";
        var logDirectory = Path.GetDirectoryName(errorLogPath);

        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory); // 创建日志文件的目录
        }

        errorWriter = new StreamWriter(errorLogPath);
        Console.SetOut(errorWriter);

        try
        {
            Write("Ra2Client更新器", ConsoleColor.Green);
            Write(string.Empty);

            //调试使用的参数
            //args = new string[] { "Ra2Client.dll", @"D:\RF-Client\Bin" };
            if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]) || !SafePath.GetDirectory(args[1].Replace("\"", null, StringComparison.OrdinalIgnoreCase)).Exists)
            {
                Write("无效参数!", ConsoleColor.Red);
                Write("格式: <client_executable_name> <base_directory>");
                Write(string.Empty);
                Write("按任意键退出更新器.");
                Console.ReadKey();
                Environment.Exit(1);
            }
            else
            {
                FileInfo clientExecutable = SafePath.GetFile(args[0]);
                DirectoryInfo baseDirectory = SafePath.GetDirectory(args[1].Replace("\"", null, StringComparison.OrdinalIgnoreCase));

                Write("根目录: " + baseDirectory.FullName);
                Write("正在等待客户端(" + clientExecutable.Name + ")退出...");

                string clientMutexId = FormattableString.Invariant($"Global{Guid.Parse("4C2EC0A0-94FB-4075-953D-8A3F62E490AA")}");
                using var clientMutex = new Mutex(false, clientMutexId, out _);

                try
                {
                    clientMutex.WaitOne(-1, false);
                }
                catch (AbandonedMutexException)
                {
                }

                DirectoryInfo updaterDirectory = SafePath.GetDirectory(baseDirectory.FullName, "Tmp");
                if (!updaterDirectory.Exists)
                {
                    Write($"{updaterDirectory.Name} 目录不存在!", ConsoleColor.Red);
                    Write("按任意键退出更新器.");
                    Console.ReadKey();
                    Environment.Exit(1);
                }

                Write("开始更新文件.", ConsoleColor.Green);

                IEnumerable<FileInfo> files = updaterDirectory.EnumerateFiles("*", SearchOption.AllDirectories);
                FileInfo executableFile = SafePath.GetFile(Assembly.GetExecutingAssembly().Location);
                FileInfo relativeExecutableFile = SafePath.GetFile(executableFile.FullName[baseDirectory.FullName.Length..]);

                FileInfo delUpdateFile = files.FirstOrDefault(file => file.Name.Equals("delUpdate", StringComparison.OrdinalIgnoreCase));
                if (delUpdateFile != null)
                {
                    DeleteListedFiles(baseDirectory, delUpdateFile);
                }

                const int maxRetryCount = 10;
                const int retryDelay = 1000; // 1秒

                // 先统一检查所有文件是否被占用
                bool anyFileInUse = files.Any(fileInfo => IsFileInUse(fileInfo));
                bool bUpdateSuc = true;
                int retryCount = 0;

                // 如果文件被占用，最多尝试10次，每次间隔1秒
                while (anyFileInUse && retryCount < maxRetryCount)
                {
                    Write("发现文件被占用，等待 1 秒后重新检查...", ConsoleColor.Yellow);
                    Thread.Sleep(retryDelay);
                    anyFileInUse = files.Any(fileInfo => IsFileInUse(fileInfo));
                    retryCount++;
                }

                if (anyFileInUse)
                {
                    Write("文件仍然被占用，无法完成更新。", ConsoleColor.Red);
                }
                else
                {
                    // 如果没有文件被占用，进行文件更新操作
                    foreach (FileInfo fileInfo in files)
                    {
                        FileInfo relativeFileInfo = SafePath.GetFile(fileInfo.FullName[updaterDirectory.FullName.Length..]);
                        AssemblyName[] assemblies = Assembly.LoadFrom(executableFile.FullName).GetReferencedAssemblies();

                        // 检查文件是否为当前更新程序或其依赖项
                        if (relativeFileInfo.ToString()[..^relativeFileInfo.Extension.Length].Equals(relativeExecutableFile.ToString()[..^relativeExecutableFile.Extension.Length], StringComparison.OrdinalIgnoreCase)
                            || relativeFileInfo.ToString()[..^relativeFileInfo.Extension.Length].Equals(SafePath.CombineFilePath("Resources", Path.GetFileNameWithoutExtension(relativeExecutableFile.Name)), StringComparison.OrdinalIgnoreCase))
                        {
                            Write($"跳过 {nameof(ClientUpdater)} 文件 {relativeFileInfo}");
                        }
                        else if (assemblies.Any(q => relativeFileInfo.ToString()[..^relativeFileInfo.Extension.Length].Equals(q.Name, StringComparison.OrdinalIgnoreCase))
                            || assemblies.Any(q => relativeFileInfo.ToString()[..^relativeFileInfo.Extension.Length].Equals(SafePath.CombineFilePath("Resources", q.Name), StringComparison.OrdinalIgnoreCase)))
                        {
                            Write($"跳过 {nameof(ClientUpdater)} 依赖 {relativeFileInfo}");
                        }
                        else
                        {
                            try
                            {
                                FileInfo copiedFile = SafePath.GetFile(baseDirectory.FullName, relativeFileInfo.ToString());
                                Write($"更新文件 -> {relativeFileInfo}");

                                Directory.CreateDirectory(Path.GetDirectoryName(copiedFile.FullName));
                                fileInfo.CopyTo(copiedFile.FullName, true);
                            }
                            catch (IOException ex)
                            {
                                Write($"更新文件失败: {ex}", ConsoleColor.Yellow);
                                bUpdateSuc = false;
                                break;
                            }
                        }
                    }
                }

                if (updaterDirectory.Exists)
                {
                    Directory.Delete(updaterDirectory.FullName, true);
                }

                if (bUpdateSuc)
                {
                    Write("文件已经全部更新成功. 正在启动主程序..", ConsoleColor.Green);
                }
                else
                {
                    Write("更新失败");
                }

                string launcherExe = clientExecutable.Name;
                FileInfo launcherExeFile = SafePath.GetFile(baseDirectory.FullName, "Resources", "Binaries", launcherExe);

                if (launcherExeFile.Exists)
                {
                    Write("发现启动程序: " + launcherExeFile.FullName, ConsoleColor.Green);

#pragma warning disable SA1312 // Variable names should begin with lower-case letter

                    string strDotnet = @"C:\Program Files (x86)\dotnet\dotnet.exe";
                    if (Environment.Is64BitProcess)
                    {
                        strDotnet = @"C:\Program Files\dotnet\dotnet.exe";
                    }

                    using var _ = Process.Start(new ProcessStartInfo
                    {
                        FileName = strDotnet,
                        Arguments = "\"" + launcherExeFile.FullName + "\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                    });
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
                }
                else
                {
                    Write($"当前启动主程序：{launcherExeFile.FullName}");
                    Write("没有找到启动主程序，更新器将不再进行任何程序的启动任务。", ConsoleColor.Yellow);
                    Write("按任意键退出更新器.");
                    Console.ReadKey();
                    Environment.Exit(1);
                }
            }
        }
        catch (IOException ex)
        {
            Write("An error occured during the Launcher Updater's operation.", ConsoleColor.Red);
            Write($"Returned error was: {ex}");
            Write(string.Empty);
            Write("If you were updating a game, please try again. If the problem continues, contact the staff for support.");
            Write("Press any key to exit.");
            Console.ReadKey();
            Environment.Exit(1);
        }

        errorWriter.Close();
    }

    /// <summary>
    /// 更新前需要删的文件
    /// </summary>
    /// <param name="delUpdateFile">文件列表</param>
    private static void DeleteListedFiles(DirectoryInfo directoryInfo, FileInfo delUpdateFile)
    {
        if (delUpdateFile.Exists)
        {
            string[] lines = File.ReadAllLines(delUpdateFile.FullName);
            foreach (string line in lines)
            {
                string path = Path.Combine(directoryInfo.FullName, line);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Console.WriteLine($"删除文件: {path}");
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    Console.WriteLine($"删除目录: {path}");
                }
                else
                {
                    Console.WriteLine($"路径不存在: {path}");
                }
            }

            // 删除 delUpdate 文件本身
            delUpdateFile.Delete();

            // Console.WriteLine($"删除文件: {delUpdateFile.FullName}");
        }
    }

    private static bool IsFileInUse(FileInfo file)
    {
        try
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                fs.Close(); // 文件可用，正常关闭流
            }

            return false; // 文件未被占用
        }
        catch (IOException)
        {
            return true; // 捕获IOException表示文件被占用
        }
    }

    private static void Write(string text)
    {
        Console.ForegroundColor = defaultColor;
        Console.WriteLine(text);
    }

    private static void Write(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = defaultColor;
    }
}