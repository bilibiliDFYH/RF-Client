using ClientCore;
using ClientGUI;
using CNCMaps.Engine;
using CNCMaps.Shared;
using DTAConfig.OptionPanels;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Timers.Timer;
/* !! We cannot use references to other projects or non-framework assemblies in this class, assembly loading events not hooked up yet !! */

namespace Ra2Client
{
    static class Program
    {
        static Program()
        {
            /* We have different binaries depending on build platform, but for simplicity
             * the target projects (DTA, TI, MO, YR) supply them all in a single download.
             * To avoid DLL hell, we load the binaries from different directories
             * depending on the build platform. */

            string startupPath = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.Parent.Parent.FullName + Path.DirectorySeparatorChar;

            COMMON_LIBRARY_PATH = Path.Combine(startupPath, "Resources", "Binaries") + Path.DirectorySeparatorChar;

            SPECIFIC_LIBRARY_PATH = Path.Combine(startupPath, "Resources", "Binaries") + Path.DirectorySeparatorChar;

            // Set up DLL load paths as early as possible
            AssemblyLoadContext.Default.Resolving += DefaultAssemblyLoadContextOnResolving;
        }

        private static string COMMON_LIBRARY_PATH;
        private static string SPECIFIC_LIBRARY_PATH;
        private static readonly string MajorVerifyUrl = "https://www.ru2023.top/verify/launcher.txt";
        private static readonly string MinorVerifyUrl = "https://www.yra2.com/verify/launcher.txt";
      //  private static Timer checkVersionTimer;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Run(args);
        }

        private static void Run(string[] args)
        {
            CDebugView.SetDebugName("Ra2Client");
            //System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            //var ini1 = new IniFile("E:\\Documents\\file\\RF-Client\\Bin\\spawnmap.ini");
            //var ini2 = new IniFile("E:\\Documents\\file\\RF-Client\\Bin\\Client\\custom_rules_all.ini");
            //IniFile.ConsolidateIniFiles(ini1,ini2);
            //ini1.WriteIniFile("E:\\Documents\\file\\RF-Client\\Bin\\test2.ini",Encoding.GetEncoding("Big5"));

            bool noAudio = false;
            bool multipleInstanceMode = false;
            List<string> unknownStartupParams = new List<string>();

            for (int arg = 0; arg < args.Length; arg++)
            {
                string argument = args[arg].ToUpperInvariant();

                switch (argument)
                {
                    case "-NOAUDIO":
                        noAudio = true;
                        break;
                    case "-MULTIPLEINSTANCE":
                        multipleInstanceMode = true;
                        break;
                    case "-NOLOGO":
                        ProgramConstants.SkipLogo = true;
                        break;
                    default:
                        unknownStartupParams.Add(argument);
                        break;
                }
            }

            if (!Directory.Exists("Resources/Dynamicbg"))
                ProgramConstants.SkipLogo = true;

            var parameters = new StartupParams(noAudio, multipleInstanceMode, unknownStartupParams);

            bool canStart = CheckVersion().Result;
            if (!canStart)
            {
                string content = new HttpClient().GetStringAsync(MajorVerifyUrl).Result;
                string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                Version minVersion = new Version(lines.First().Trim());
                Version maxVersion = new Version(lines.Last().Trim());
                string currentVersion = Assembly.GetAssembly(typeof(Program)).GetName().Version.ToString();
                Version current = new Version(currentVersion);

                if (current < minVersion)
                {
                    MessageBox.Show("检测到铁幕装置已离线, 当前版本已停止维护!\n请到重聚未来官网 www.yra2.com 更新最新客户端", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                }
                else if (current > maxVersion)
                {
                    MessageBox.Show("检测到超时空坐标受到干扰, 当前版本暂未开放!\n请到重聚未来官网 www.yra2.com 获取更多信息", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                }
                Environment.Exit(1);
            }

            //checkVersionTimer = new Timer(1 * 60 * 1000);
            //checkVersionTimer.Elapsed += async (sender, e) => await ReCheckVer();
            //checkVersionTimer.AutoReset = true;
            //checkVersionTimer.Enabled = true;

            if (multipleInstanceMode)
            {
                // Proceed to client startup
                PreStartup.Initialize(parameters);
                return;
            }

            // We're a single instance application!
            // http://stackoverflow.com/questions/229565/what-is-a-good-pattern-for-using-a-global-mutex-in-c/229567
            // Global prefix means that the mutex is global to the machine
            string mutexId = FormattableString.Invariant($"Global{Guid.Parse("4C2EC0A0-94FB-4075-953D-8A3F62E490AA")}");
            using var mutex = new Mutex(false, mutexId, out _);
            bool hasHandle = false;

            try
            {
                try
                {
                    hasHandle = mutex.WaitOne(8000, false);
                    if (hasHandle == false)
                        throw new TimeoutException("Timeout waiting for exclusive access");
                }
                catch (AbandonedMutexException)
                {
                    hasHandle = true;
                }
                catch (TimeoutException)
                {
                    return;
                }

                // Proceed to client startup
                PreStartup.Initialize(parameters);
            }
            finally
            {
                if (hasHandle)
                    mutex.ReleaseMutex();
            }
        }

        private static async Task<bool> CheckVersion()
        {
            try
            {
                using HttpClient client = new HttpClient();
                string content = await client.GetStringAsync(MajorVerifyUrl);
                return ParseVersionContent(content);
            }
            catch (Exception MajorEx)
            {
                Logger.Log("Failed to check version from Major Server. " + MajorEx.Message);
                try
                {
                    using HttpClient client = new HttpClient();
                    string content = await client.GetStringAsync(MinorVerifyUrl);
                    return ParseVersionContent(content);
                }
                catch (Exception MinorEx)
                {
                    Logger.Log("Failed to check version from Minor Server. " + MinorEx.Message);
                }
            }

            return true;
        }

        private static bool ParseVersionContent(string content)
        {
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
                return true;

            string currentVersion = Assembly.GetAssembly(typeof(Program)).GetName().Version.ToString();
            Version current = new Version(currentVersion);
            Version minVersion = new Version(lines.First().Trim());
            Version maxVersion = new Version(lines.Last().Trim());

            if (current < minVersion)
            {
                return false;
            }
            else if (current > maxVersion)
            {
                return false;
            }

            return true;
        }

        private static async Task ReCheckVer()
        {
            bool canStart = await CheckVersion();
            if (!canStart)
            {
                string content = await new HttpClient().GetStringAsync(MajorVerifyUrl);
                string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                Version minVersion = new Version(lines.First().Trim());
                Version maxVersion = new Version(lines.Last().Trim());
                string currentVersion = Assembly.GetAssembly(typeof(Program)).GetName().Version.ToString();
                Version current = new Version(currentVersion);

                if (current < minVersion)
                {
                    MessageBox.Show("检测到铁幕装置已离线, 当前版本已停止维护!\n请到重聚未来官网 www.yra2.com 更新最新客户端", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                }
                else if (current > maxVersion)
                {
                    MessageBox.Show("检测到超时空坐标受到干扰, 当前版本暂未开放!\n请到重聚未来官网 www.yra2.com 获取更多信息", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                }
                Environment.Exit(1);
            }
        }

        private static Assembly DefaultAssemblyLoadContextOnResolving(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName)
        {
            if (assemblyName.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                return null;

            var commonFileInfo = new FileInfo(Path.Combine(COMMON_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));

            if (commonFileInfo.Exists)
                return assemblyLoadContext.LoadFromAssemblyPath(commonFileInfo.FullName);

            var specificFileInfo = new FileInfo(Path.Combine(SPECIFIC_LIBRARY_PATH, FormattableString.Invariant($"{assemblyName.Name}.dll")));

            if (specificFileInfo.Exists)
                return assemblyLoadContext.LoadFromAssemblyPath(specificFileInfo.FullName);

            return null;
        }
    }
}