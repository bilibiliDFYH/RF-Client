using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ClientCore;
using ClientCore.INIProcessing;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace ClientGUI
{
    /// <summary>
    /// A static class used for controlling the launching and exiting of the game executable.
    /// </summary>
    public static class GameProcessLogic
    {
        public static event Action GameProcessStarted;

        public static event Action GameProcessStarting;

        public static event Action GameProcessExited;

        public static bool UseQres { get; set; }
        public static bool SingleCoreAffinity { get; set; }

        private static string gameExecutableName;
        /// <summary>
        /// Starts the main game process.
        /// </summary>
        public static void StartGameProcess(WindowManager windowManager)
        {
            Logger.Log("About to launch main game executable.");

            int waitTimes = 0;
            while (PreprocessorBackgroundTask.Instance.IsRunning)
            {
                Thread.Sleep(1000);
                waitTimes++;
                if (waitTimes > 10)
                {
                    XNAMessageBox.Show(windowManager, "INI preprocessing not complete", "INI preprocessing not complete. Please try " +
                        "launching the game again. If the problem persists, " +
                        "contact the game or mod authors for support.");
                    return;
                }
            }

            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            
            string additionalExecutableName = string.Empty;

            string launcherExecutableName = ClientConfiguration.Instance.GameLauncherExecutableName;
            if (string.IsNullOrEmpty(launcherExecutableName))
                gameExecutableName = ClientConfiguration.Instance.GetGameExecutableName();
            else
            {
                gameExecutableName = launcherExecutableName;
                additionalExecutableName = "\"" + ClientConfiguration.Instance.GetGameExecutableName() + "\" ";
            }

            string extraCommandLine = ClientConfiguration.Instance.ExtraExeCommandLineParameters;

            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "DTA.LOG");
            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "TI.LOG");
            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "TS.LOG");

            GameProcessStarting?.Invoke();

            if (UserINISettings.Instance.WindowedMode && UseQres && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Logger.Log("Windowed mode is enabled - using QRes.");
                Process QResProcess = new Process();
                QResProcess.StartInfo.FileName = ProgramConstants.QRES_EXECUTABLE;

                if (!string.IsNullOrEmpty(extraCommandLine))
                    QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + SafePath.CombineFilePath(ProgramConstants.GamePath, gameExecutableName) + "\" " + additionalExecutableName + "-SPAWN " + extraCommandLine;
                else
                    QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + SafePath.CombineFilePath(ProgramConstants.GamePath, gameExecutableName) + "\" " + additionalExecutableName + "-SPAWN";
                QResProcess.EnableRaisingEvents = true;
                QResProcess.Exited += new EventHandler(Process_Exited); 
                Logger.Log("启动命令: " + QResProcess.StartInfo.FileName);
                Logger.Log("启动参数: " + QResProcess.StartInfo.Arguments);
                try
                {
                    QResProcess.Start();
                }
                catch (Exception ex)
                {
                    Logger.Log("Error launching QRes: " + ex.Message);
                    XNAMessageBox.Show(windowManager, "Error launching game", "Error launching " + ProgramConstants.QRES_EXECUTABLE + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
                        "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." +
                        Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message);
                    Process_Exited(QResProcess, EventArgs.Empty);
                    return;
                }

                if (Environment.ProcessorCount > 1 && SingleCoreAffinity)
                    QResProcess.ProcessorAffinity = (IntPtr)2;
            }
            else
            {
                string arguments;
               
                if (!string.IsNullOrWhiteSpace(extraCommandLine))
                    arguments = " " + additionalExecutableName + "-SPAWN " + extraCommandLine;
                else
                    arguments = additionalExecutableName + "-SPAWN";

                if (File.Exists(Path.Combine(ProgramConstants.GamePath,"syringe.exe")))
                {
                    gameExecutableName = "Syringe.exe";
                    arguments = "\"gamemd.exe\" -SPAWN " + extraCommandLine;
                }
                else if (File.Exists("NPatch.mix"))
                {
                    gameExecutableName = "gamemd-np.exe";
                    arguments = "-SPAWN " + extraCommandLine;
                }
                else
                {
                    gameExecutableName = "gamemd-spawn.exe";
                    arguments = "-SPAWN " + extraCommandLine;
                }

                

                FileInfo gameFileInfo = SafePath.GetFile(ProgramConstants.GamePath, gameExecutableName);
                if (!File.Exists(gameFileInfo.FullName))
                {
                    XNAMessageBox.Show(windowManager, "错误", $"{gameFileInfo.FullName}不存在，请前往设置清理游戏缓存后重试。");
                    return;
                }

                UserINISettings.Instance.暂停渲染地图?.Invoke();

                ProcessStartInfo info = new ProcessStartInfo(gameFileInfo.FullName, arguments)
                {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var gameProcess = Process.Start(info);

                gameProcess.EnableRaisingEvents = true;
                gameProcess.Exited += Process_Exited;

                Logger.Log("启动可执行文件: " + gameProcess.StartInfo.FileName);
                Logger.Log("启动参数: " + gameProcess.StartInfo.Arguments);

                //if ((RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                //    && Environment.ProcessorCount > 1 && SingleCoreAffinity)
                //{
                //    // 设置为所有核心
                //    try
                //    {
                //        int processorCount = Environment.ProcessorCount;

                //        // 确保不超过 long 的位移限制
                //        if (processorCount >= 64)
                //        {
                //            processorCount = 63; // 限制为最大位移63，以避免溢出
                //        }

                //        long allCoresMask = (1L << processorCount) - 1;
                //        gameProcess.ProcessorAffinity = new IntPtr(allCoresMask);
                //    }
                //    catch
                //    {
                //        gameProcess.ProcessorAffinity = 2;
                //    }
                //}

                try
                {
                    gameProcess.Start();
                    Logger.Log("游戏处理逻辑: 进程开始.");
                }
                catch (Exception ex)
                {
                    Logger.Log("Error launching " + gameFileInfo.Name + ": " + ex.Message);
                    XNAMessageBox.Show(windowManager, "Error launching game", "Error launching " + gameFileInfo.Name + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
                        "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." +
                        Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message);
                    Process_Exited(gameProcess, EventArgs.Empty);
                    return;
                }

                
            }

            GameProcessStarted?.Invoke();

            Logger.Log("等待 qres.dat 或 " + gameExecutableName + " 退出.");
        }

        static void Process_Exited(object sender, EventArgs e)
        {

            
            Process proc = (Process)sender;

            var ps = Process.GetProcesses();

            try
            {
                var p = ps.FirstOrDefault(p => p.ProcessName == Path.GetFileNameWithoutExtension(gameExecutableName));
                if (p != null)
                {
                    p.EnableRaisingEvents = true;
                    p.Exited += Process_Exited;
                    return;
                }
            }
            finally
            {
                Logger.Log("GameProcessLogic: Process exited.");
                UserINISettings.Instance.继续渲染地图?.Invoke();
                proc.Exited -= Process_Exited;
                proc.Dispose();
                GameProcessExited?.Invoke();
            }
           
        }
    }
}