using ClientCore;
using ClientGUI;
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

            var checkVersionTask = Task.Run(async () => await CheckVersionFast().ConfigureAwait(false));
            (bool canStart, int versionDigits) = (true, 0);
            try
            {
                (canStart, versionDigits) = checkVersionTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Log("版本检查失败: " + ex.Message);
                canStart = true;
            }

            if (!canStart)
            {
                if (versionDigits == 3)
                {
                    MessageBox.Show("当前正式版本已停止维护! 部分功能可能无法正常使用\n请及时到重聚未来官网 www.yra2.com 更新客户端以获得后续的技术支持", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                }
                else if (versionDigits == 4)
                {
                    MessageBox.Show("当前测试版本已停止维护! 部分功能可能无法正常使用\n请及时到重聚未来 官方QQ群/微信群 更新客户端以获得后续的技术支持\n\n(群号见重聚未来官网: www.yra2.com)", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                }
                else
                {
                    MessageBox.Show("当前版本已停止维护! 部分功能可能无法正常使用\n请及时到重聚未来官网 www.yra2.com 更新客户端以获得后续的技术支持", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                }
            }

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

        /// <summary>
        /// 版本检测，3秒超时，主备并发
        /// </summary>
        private static async Task<(bool canStart, int versionDigits)> CheckVersionFast()
        {
            using HttpClient client = new HttpClient();
            var majorTask = client.GetStringAsync(MajorVerifyUrl);
            var minorTask = client.GetStringAsync(MinorVerifyUrl);

            var timeoutTask = Task.Delay(3000);
            var completedTask = await Task.WhenAny(Task.WhenAny(majorTask, minorTask), timeoutTask).ConfigureAwait(false);

            string content = null;
            if (completedTask == timeoutTask)
            {
                Logger.Log("Warning: Version list check timed out!");
                return (true, GetCurrentVersionDigits());
            }

            try
            {
                if (majorTask.IsCompletedSuccessfully)
                    content = majorTask.Result;
                else if (minorTask.IsCompletedSuccessfully)
                    content = minorTask.Result;
                else
                    content = await (await Task.WhenAny(majorTask, minorTask)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log("版本检查异常: " + ex.Message);
                return (true, GetCurrentVersionDigits());
            }

            bool canStart = ParseVersionContent(content);
            int versionDigits = GetCurrentVersionDigits();
            return (canStart, versionDigits);
        }

        /// <summary>
        /// 获取主程序的版本号位数
        /// </summary>
        private static int GetCurrentVersionDigits()
        {
            string version = GetMainProgramVersion();
            return version.Split('.').Length;
        }

        /// <summary>
        /// 获取主程序的版本号字符串
        /// </summary>
        private static string GetMainProgramVersion()
        {
            try
            {
                return Assembly.GetAssembly(typeof(Program)).GetName().Version.ToString();
            }
            catch (Exception ex)
            {
                Logger.Log("获取主程序版本失败: " + ex.Message);
                // 返回一个默认值，避免异常
                return "0.0.0";
            }
        }

        /// <summary>
        /// 检查服务器返回内容是否包含当前主程序版本
        /// </summary>
        private static bool ParseVersionContent(string content)
        {
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
                return true;

            string currentVersion = Assembly.GetAssembly(typeof(Program)).GetName().Version.ToString();

            foreach (string line in lines)
            {
                if (line.Trim().Equals(currentVersion, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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