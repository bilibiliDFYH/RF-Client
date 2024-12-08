namespace Reunion;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

internal sealed class Program
{
    private const string Resources = "Resources";
    private const string Binaries = "Binaries";
    private const int DotNetMajorVersion = 6; // 客户端.Net 版本要求

    private static readonly Uri DotNetX64RuntimeDownloadLink = new(FormattableString.Invariant($"https://aka.ms/dotnet/{DotNetMajorVersion}.0/dotnet-runtime-win-x64.exe"));
    private static readonly Uri DotNetX64DesktopRuntimeDownloadLink = new(FormattableString.Invariant($"https://aka.ms/dotnet/{DotNetMajorVersion}.0/windowsdesktop-runtime-win-x64.exe"));
    private static readonly Uri DotNetX86RuntimeDownloadLink = new(FormattableString.Invariant($"https://aka.ms/dotnet/{DotNetMajorVersion}.0/dotnet-runtime-win-x86.exe"));
    private static readonly Uri DotNetX86DesktopRuntimeDownloadLink = new(FormattableString.Invariant($"https://aka.ms/dotnet/{DotNetMajorVersion}.0/windowsdesktop-runtime-win-x86.exe"));
    
    private static readonly IReadOnlyDictionary<(Architecture Architecture, bool Desktop), Uri> DotNetDownloadLinks = new Dictionary<(Architecture Architecture, bool Desktop), Uri>
    {
        { (Architecture.X64, false), DotNetX64RuntimeDownloadLink },
        { (Architecture.X64, true), DotNetX64DesktopRuntimeDownloadLink },
        { (Architecture.X86, false), DotNetX86RuntimeDownloadLink },
        { (Architecture.X86, true), DotNetX86DesktopRuntimeDownloadLink },
    };
    private static string[] Args;

    [STAThread]
    private static void Main(string[] args)
    {

        Args = args;

        try
        {
#if DEBUG
            RunDialogTest();
#else
            RunDX();
#endif
        }
        catch (Exception ex)
        {
            AdvancedMessageBoxHelper.ShowOkMessageBox(ex.ToString(), "客户端启动异常", okText: "Exit");
            Environment.Exit(1);
        }
    }

    private static void RunDialogTest()
    {
        var msgbox = new AdvancedMessageBox();
        var model = (AdvancedMessageBoxViewModel)msgbox.DataContext;
        model.Title = "客户端启动测试";
        model.Message = "点击以下按钮测试错误效果：";
        model.Commands = new ObservableCollection<CommandViewModel>()
        {
            new CommandViewModel()
            {
                Text = "显示不兼容GPU对话框",
                Command = new RelayCommand(_ => ShowIncompatibleGPUMessage(new[] { "打开链接 (所有按钮均不工作)", "启动 DirectX11 版本", "退出" })),
            },

            new CommandViewModel()
            {
                Text = "显示丢失组件对话框",
                Command = new RelayCommand(_ => ShowMissingComponent("组件名称", new Uri("https://www.yra2.com"))),
            },

            new CommandViewModel()
            {
                Text = "抛出错误",
                Command = new RelayCommand(_ => throw new Exception("异常消息")),
            },

            new CommandViewModel()
            {
                Text = "正常启动客户端",
                Command = new RelayCommand(_ => RunDX()),
            },

            new CommandViewModel()
            {
                Text = "退出",
                Command = new RelayCommand(_ => msgbox.Close()),
            },
        };
        msgbox.ShowDialog();
    }

    private static void RunDX() => StartProcess(GetClientProcessPath("Ra2Client.dll"));

    private static string GetClientProcessPath(string file) => $"{Resources}\\{Binaries}\\{file}";

    private static int? ShowIncompatibleGPUMessage(string[] selections)
    {
        return AdvancedMessageBoxHelper.ShowMessageBoxWithSelection(
            string.Format(
                "客户端检测到您的图形卡与Reunion客户端的DirectX11版本不兼容\n" +
                "您可以尝试启动客户端的DirectX11版本。\n" +
                "我们对给您带来的不便表示歉意。"),
            "不兼容信息",
            selections);
    }

    private static void StartProcess(string relPath, bool run32Bit = false, bool runDesktop = true)
    {
        try
        {
            // if (Environment.Is64BitOperatingSystem)
            //    run32Bit = true;
            FileInfo dotnetHost = CheckAndRetrieveDotNetHost(run32Bit ? Architecture.X86 : RuntimeInformation.OSArchitecture, runDesktop);
            string absPath = $"{Environment.CurrentDirectory}\\{relPath}";

            if (!File.Exists(absPath))
            {
                AdvancedMessageBoxHelper.ShowOkMessageBox($"客户端入口 ({relPath}) 不存在!", "客户端启动异常", okText: "退出");
                Environment.Exit(3);
            }

            OperatingSystem os = Environment.OSVersion;

            // Required on Win7 due to W^X causing issues there.
            if (os.Platform == PlatformID.Win32NT && os.Version.Major == 6 && os.Version.Minor == 1)
            {
                Environment.SetEnvironmentVariable("DOTNET_EnableWriteXorExecute", "0");
            }

            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = dotnetHost.FullName,
                Arguments = "\"" + absPath + "\" " + string.Join(" ", Args.Select(arg => "\"" + arg + "\"")),
                CreateNoWindow = true,
                UseShellExecute = false,
            });
        }
        catch (Exception ex)
        {
            AdvancedMessageBoxHelper.ShowOkMessageBox($"启动客户端异常：{ex.Message}", "客户端启动异常", okText: "退出");
            Environment.Exit(4);
        }
    }

    private static FileInfo CheckAndRetrieveDotNetHost(Architecture machineArchitecture, bool runDesktop)
    {
        // Architectures to be searched for
        List<Architecture> architectures = new() { machineArchitecture };

        // Search for installed dotnet architectures
        Architecture? availableArchitecture = null;
        foreach (Architecture architecture in architectures)
        {
            if (IsDotNetCoreInstalled(architecture)
                && (!runDesktop || IsDotNetDesktopInstalled(architecture)))
            {
                availableArchitecture = architecture;
                break;
            }
        }

        // Prompt the download link and terminate the program if no architectures are available
        if (availableArchitecture is null)
        {
            string missingComponent = runDesktop
                ? $".NET Desktop Runtime{DotNetMajorVersion}_{machineArchitecture}"
                : $".NET Runtime{DotNetMajorVersion}_{machineArchitecture}";
            ShowMissingComponent(missingComponent, DotNetDownloadLinks[(machineArchitecture, runDesktop)]);
            Environment.Exit(2);
            return null;
        }
        else
        {
            FileInfo? dotnetHost = GetDotNetHost(availableArchitecture.GetValueOrDefault());
            return dotnetHost!;
        }
    }

    private static void OpenUri(Uri uri)
    {
        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = uri.ToString(),
            UseShellExecute = true,
        });
    }

    private static void ShowMissingComponent(string missingComponent, Uri downloadLink)
    {
        bool dialogResult = AdvancedMessageBoxHelper.ShowYesNoMessageBox(
            string.Format(
            "组件 \"{0}\" 丢失.\n" +
            "手动下载地址:https://alist.yra2.com\n" +
            "您也可以通过以下链接进行安装:\n\n{1}",
            missingComponent, downloadLink.ToString()), "组件丢失",
            yesText: "打开链接", noText: "退出");
        if (dialogResult)
            OpenUri(downloadLink);
    }

    private static FileInfo? GetDotNetHost(Architecture architecture)
    {
        if (!IsDotNetCoreInstalled(architecture))
            return null;

        using var localMachine32BitRegistryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        using RegistryKey? dotnetArchitectureKey = localMachine32BitRegistryKey.OpenSubKey(
            $"SOFTWARE\\dotnet\\Setup\\InstalledVersions\\{architecture}");
        string? installLocation = dotnetArchitectureKey?.GetValue("InstallLocation")?.ToString();

        return installLocation is null ? null : new FileInfo($"{installLocation}\\dotnet.exe");
    }

    private static bool IsDotNetCoreInstalled(Architecture architecture)
        => IsDotNetInstalled(architecture, "Microsoft.NETCore.App");

    private static bool IsDotNetDesktopInstalled(Architecture architecture)
        => IsDotNetInstalled(architecture, "Microsoft.WindowsDesktop.App");

    private static bool IsDotNetInstalled(Architecture architecture, string sharedFrameworkName)
    {
        using var localMachine32BitRegistryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        using RegistryKey? dotnetSharedFrameworkKey = localMachine32BitRegistryKey.OpenSubKey(
            $"SOFTWARE\\dotnet\\Setup\\InstalledVersions\\{architecture}\\sharedfx\\{sharedFrameworkName}");

        return dotnetSharedFrameworkKey?.GetValueNames().Any(q =>
            q.StartsWith($"{DotNetMajorVersion}.", StringComparison.OrdinalIgnoreCase)
            && !q.Contains('-')
            && "1".Equals(dotnetSharedFrameworkKey.GetValue(q)?.ToString(), StringComparison.OrdinalIgnoreCase)) ?? false;
    }
}