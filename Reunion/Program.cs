using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Windows.Forms;

namespace Reunion
{
    internal class Program
    {
        private const int DotNetMajorVersion = 6; // 客户端.Net 版本要求
        private const string Resources = "Resources";
        private const string Binaries = "Binaries";

        private static string sharedPath64 = @"C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App";

        private static string dotnet = @"C:\Program Files\dotnet\dotnet.exe";

        private static string[] Args;
        static void Main(string[] args)
        {
            Args = args;
            StartProcess(GetClientProcessPath("Ra2Client.dll"));
        }
        private static string GetClientProcessPath(string file) => $"{Resources}\\{Binaries}\\{file}";

        private static void StartProcess(string relPath)
        {
            try
            {
                // if (Environment.Is64BitOperatingSystem)
                //    run32Bit = true;
                var dotnetHost = CheckAndRetrieveDotNetHost();

                if(dotnetHost == null)
                {
                    MessageBox.Show($"缺少 NET 6 组件 ，请下载对应计算机位数的 NET 6 组件", "错误");
                    return;
                }

                string absPath = $"{Environment.CurrentDirectory}\\{relPath}";

                if (!File.Exists(absPath))
                {
                    _ = MessageBox.Show($"客户端入口 ({relPath}) 不存在!", "客户端启动异常");
                    Environment.Exit(3);
                }

                OperatingSystem os = Environment.OSVersion;

                // Required on Win7 due to W^X causing issues there.
                if (os.Platform == PlatformID.Win32NT && os.Version.Major == 6 && os.Version.Minor == 1)
                {
                    Environment.SetEnvironmentVariable("DOTNET_EnableWriteXorExecute", "0");
                }


                foreach (var zip in Directory.GetFiles("./", "Updater*.7z"))
                {
                    ZIP.SevenZip.ExtractWith7Zip(zip, "./", needDel: true);
                }

                var Arguments = "\"" + absPath + "\" " + GetArguments(Args);

                Console.WriteLine(dotnetHost);
                Console.WriteLine(Arguments);

                Process p = Process.Start(new ProcessStartInfo
                {
                    FileName = dotnetHost,
                    Arguments = Arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    Verb = "runas",
                });
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"启动客户端异常：{ex.Message}", "客户端启动异常");
                Environment.Exit(4);
            }
        }

        private static string GetArguments(string[] args)
        {
            string result = string.Empty;

            // 使用 foreach 代替 LINQ 的 Select
            foreach (var arg in args)
            {
                result += "\"" + arg + "\" ";
            }

            return result.Trim(); // 去掉最后的空格
        }

        private static string CheckAndRetrieveDotNetHost()
        {
          
            var r = FindDotNet6InPath(sharedPath64);
            if(r == null)
            {
                return null;
            }
            else
            {
                return dotnet;
            }

           
        }

        private static string FindDotNet6InPath(string path)
        {
            // 检查指定路径是否存在
            if (Directory.Exists(path))
            {
                // 获取该目录下所有文件夹
                var directories = Directory.GetDirectories(path);

                foreach (var dir in directories)
                {
                    var folderName = Path.GetFileName(dir);

                    // 如果文件夹名以 "6" 开头，返回该文件夹路径
                    if (folderName.StartsWith(DotNetMajorVersion.ToString()))
                    {
                        return dir;
                    }
                }
            }
            return null; // 未找到以 6 开头的文件夹
        }
    }
}
