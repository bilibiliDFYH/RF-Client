using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Org.BouncyCastle.Crypto.Digests;

namespace Reunion
{
    internal class Program
    {
        private const string Resources = "Resources";
        private const string Binaries = "Binaries";
        private const string LicenseFile = "License-GPLv3.txt";
        private const string RequiredFile = "使用前必读.txt";
        private const string FreeFile = "本程序完全免费，倒卖者s全家.txt";

        private const string ExpectedHash_LicenseFile = "dc447a64136642636d7aa32e50c76e2465801c5f";
        private const string ExpectedHash_RequiredFile = "3890356c3f346e72d5feab11dbea71c26ce3c22879b2fe8d580fe3edc7a29462";
        private const string ExpectedHash_FreeFile = "3f4f36d5bc8d62a18e9fce9bc67744320afb46bfd49de32bc99277bb84916410";

        // 动态获取系统盘符, 防止多系统情况下无法启动
        private static readonly string SystemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
        private static readonly string dotnetPath = Path.Combine(SystemDrive, "Program Files", "dotnet");
        private static readonly string dotnetPathA64 = Path.Combine(SystemDrive, "Program Files", "dotnet", "x64");

        private static string[] Args;
        static void Main(string[] args)
        {
            Args = args;

            if (!CheckRequiredFile())
            {
                return;
            }

            StartProcess(GetClientProcessPath("Ra2Client.dll"));
        }

        private static bool CheckRequiredFile()
        {
            if (!File.Exists(RequiredFile) || !File.Exists(FreeFile) || !File.Exists(LicenseFile))
            {
                MessageBox.Show("发现未知错误，请联系重聚未来制作组", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                string actualHash1 = ComputeFileSHA256(RequiredFile);
                if (!actualHash1.Equals(ExpectedHash_RequiredFile, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("发现未知错误，请联系重聚未来制作组", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                string actualHash2 = ComputeFileSHA3_256(FreeFile);
                if (!actualHash2.Equals(ExpectedHash_FreeFile, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("发现未知错误，请联系重聚未来制作组", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                string actualHash3 = ComputeFileSHA1(LicenseFile);
                if (!actualHash3.Equals(ExpectedHash_LicenseFile, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("发现未知错误，请联系重聚未来制作组", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"文件校验出错: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 计算文件的SHA1哈希值
        /// </summary>
        private static string ComputeFileSHA1(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hashBytes = sha1.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// 计算文件的SHA256哈希值
        /// </summary>
        private static string ComputeFileSHA256(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// 计算文件的SHA3_256哈希值
        /// </summary>
        private static string ComputeFileSHA3_256(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                var sha3 = new Sha3Digest(256);
                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    sha3.BlockUpdate(buffer, 0, bytesRead);
                }
                byte[] result = new byte[sha3.GetDigestSize()];
                sha3.DoFinal(result, 0);
                return BitConverter.ToString(result).Replace("-", "").ToLowerInvariant();
            }
        }

        private static string GetClientProcessPath(string file) => $"{Resources}\\{Binaries}\\{file}";

        private static void StartProcess(string relPath)
        {
            try
            {
                var dotnetHost = CheckAndRetrieveDotNetHost();

                if (dotnetHost == null)
                {
                    string arch = RuntimeInformation.OSArchitecture.ToString().ToLower();
                    string message;
                    string url;

                    // 判断是否为中国大陆
                    string countryCode = GetCountryCodeByIp();
                    string domain;
                    if (string.IsNullOrEmpty(countryCode))
                    {
                        domain = "files-cn-v4.ru2023.top/directlink-v2/x";
                    }
                    else
                    {
                        domain = (countryCode == "CN") ? "files-cn-v4.yra2.com/directlink-v1/y" : "files-global-v4.yra2.net/directlink-v5/z";
                    }

                    switch (arch)
                    {
                        case "x86":
                            message = "检测到缺少所需的.NET6 x86运行环境, 是否立即跳转到重聚未来官网进行下载?\n\n所需运行时版本要求: v6.0.10 - v6.0.36";
                            url = $"https://{domain}/NET6/x86/windowsdesktop-runtime-6.0.36-win-x86.exe";
                            break;
                        case "x64":
                            message = "检测到缺少所需的.NET6 x64运行环境, 是否立即跳转到重聚未来官网进行下载?\n\n所需运行时版本要求: v6.0.10 - v6.0.36";
                            url = $"https://{domain}/NET6/x64/windowsdesktop-runtime-6.0.36-win-x64.exe";
                            break;
                        case "arm64":
                            message = "检测到缺少所需的.NET6 ARM64运行环境, 是否立即跳转到重聚未来官网进行下载?\n\n所需运行时版本要求: v6.0.10 - v6.0.36";
                            url = $"https://{domain}/NET6/arm64/windowsdesktop-runtime-6.0.36-win-arm64.exe";
                            break;
                        default:
                            message = "检测到缺少所需的.NET6运行环境, 是否立即跳转到重聚未来官网进行下载?\n\n所需运行时版本要求: v6.0.10 - v6.0.36";
                            url = "https://www.yra2.com/runtime#net6-download";
                            break;
                    }
                    var result = MessageBox.Show(message, "错误", MessageBoxButtons.OKCancel);
                    if (result == DialogResult.OK)
                    {
                        Process.Start(url);
                    }
                    Environment.Exit(1);
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
                    WorkingDirectory = Environment.CurrentDirectory // 指定运行目录
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
            string arch = RuntimeInformation.OSArchitecture.ToString().ToLower();

            var pathsToCheck = new List<string>();

            // 对于x64架构添加两个检查路径
            if (arch == "x64")
            {
                pathsToCheck.Add(dotnetPath);
                pathsToCheck.Add(dotnetPathA64);
            }
            else
            {
                pathsToCheck.Add(dotnetPath);
            }

            // 遍历所有需要检查的路径
            foreach (var basePath in pathsToCheck)
            {
                string sharedRuntimePath = Path.Combine(basePath, "shared", "Microsoft.WindowsDesktop.App");
                string dotnetExePath = Path.Combine(basePath, "dotnet.exe");

                // 检查 dotnet.exe 是否存在
                if (!File.Exists(dotnetExePath))
                    continue;

                // 检查运行时目录是否存在
                if (!Directory.Exists(sharedRuntimePath))
                    continue;

                // 检查运行时版本 (6.0.10 ≤ version ≤ 6.0.36)
                foreach (var versionDir in Directory.GetDirectories(sharedRuntimePath))
                {
                    string versionName = Path.GetFileName(versionDir);

                    if (Version.TryParse(versionName, out Version version) &&
                        version.Major == 6 && version.Minor == 0 && version.Build >= 10)
                    {
                        return dotnetExePath; // 返回有效的 dotnet.exe 路径
                    }
                }
            }

            return null; // 未找到符合条件的运行时
        }

        /// <summary>
        /// 通过 curl 获取本机IP的国家代码(countryCode)，CN为中国大陆，其他为港澳台及海外
        /// </summary>
        /// <returns>国家代码，如"CN"、"HK"、"US"等，获取失败返回空字符串</returns>
        private static string GetCountryCodeByIp()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c curl -s https://api.mir6.com/api/ip_json?ip=myip",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // 提取 data 块里的 countryCode 字段
                    var match = Regex.Match(output, @"""data""\s*:\s*\{[^}]*?""countryCode"":\s*""([^""]+)""");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            catch
            {
                // 忽略异常，默认返回空
            }
            return string.Empty;
        }
    }
}