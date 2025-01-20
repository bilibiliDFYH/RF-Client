using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ClientCore;
using ClientCore.INIProcessing;
using Localization.Tools;
using Rampastring.Tools;
using Rampastring.XNAUI;
using SharpDX.XAudio2;

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

        public static Dictionary<string, Dictionary<string, string>> FileHash = [];
        public static Dictionary<string, string> FilePaths = [];
        private static string[] oldSaves;

        
        /// <summary>
        /// Starts the main game process.
        /// </summary>
        /// 
        public static void StartGameProcess(WindowManager windowManager,IniFile iniFile = null)
        {

            UserINISettings.Instance.取消渲染地图?.Invoke();
            string r = 切换文件(iniFile.GetSection("Settings"));
            if (r != string.Empty)
            {
                XNAMessageBox.Show(windowManager, "错误", r);
                return;
            }


            spawnerSettingsFile.Delete();
            iniFile.WriteIniFile(spawnerSettingsFile.FullName);

            oldSaves = Directory.GetFiles($"{ProgramConstants.GamePath}Saved Games");

            WindowManager.progress.Report("正在唤起游戏");

            

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
               // QResProcess.Exited += new EventHandler(Process_Exited); 

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

                UserINISettings.Instance.取消渲染地图?.Invoke();

                ProcessStartInfo info = new ProcessStartInfo(gameFileInfo.FullName, arguments)
                {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                var gameProcess = new Process
                {
                    StartInfo = info,
                    EnableRaisingEvents = true
                };

                // 注册退出事件
                gameProcess.Exited += Process_Exited;

                Logger.Log("启动可执行文件: " + gameProcess.StartInfo.FileName);
                Logger.Log("启动参数: " + gameProcess.StartInfo.Arguments);


                try
                {
                    gameProcess.Start();
                    WindowManager.progress.Report("游戏进行中....");
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

        static readonly FileInfo spawnerSettingsFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);
        private static void 加载音乐()
        {
            Mix.PackToMix($"{ProgramConstants.GamePath}Resources/thememd/", "./thememd.mix");
            if (File.Exists("ra2md.csf"))
            {
                var d = new CSF("ra2md.csf").GetCsfDictionary();
                if (d != null)
                {
                    foreach (var item in UserINISettings.Instance.MusicNameDictionary.Keys)
                    {
                        if (d.ContainsKey(item))
                        {
                            d[item] = UserINISettings.Instance.MusicNameDictionary[item];
                        }
                        else
                        {
                            d.Add(item, UserINISettings.Instance.MusicNameDictionary[item]);
                        }

                    }
                    CSF.WriteCSF(d, "ra2md.csf");
                }

            }
        }
        private static void 获取新的存档()
        {
            var newSaves = Directory.GetFiles($"{ProgramConstants.GamePath}Saved Games");

            if (oldSaves.Length < newSaves.Length)
            {

                var iniFile = new IniFile($"{ProgramConstants.GamePath}Saved Games/Save.ini");
                var spawn = new IniFile($"{ProgramConstants.GamePath}spawn.ini");
                var game = spawn.GetValue("Settings", "Game", string.Empty);
                var main = spawn.GetValue("Settings", "Main", string.Empty);
                var mission = spawn.GetValue("Settings", "Mission", string.Empty);
                var extension = spawn.GetValue("Settings", "Extension", string.Empty);
                var ra2Mode = spawn.GetValue("Settings", "RA2Mode", false);
                var YR_to_RA2 = spawn.GetValue("Settings", "YR_to_RA2", false);
                // 找到在 newSaves 中但不在 oldSaves 中的文件
                var addedFiles = newSaves.Where(newFile => !oldSaves.Contains(newFile)).ToArray();

                foreach (var fileFullPath in addedFiles)
                {
                    string fileName = Path.GetFileName(fileFullPath);

                    iniFile.SetValue(fileName, "Game", game);
                    iniFile.SetValue(fileName, "Extension", extension);
                    iniFile.SetValue(fileName, "Main", main);
                    iniFile.SetValue(fileName, "Mission", mission);
                    iniFile.SetValue(fileName, "RA2Mode", YR_to_RA2);
                }
                iniFile.WriteIniFile();
            }
        }
        public static string 切换文件(IniSection newSection)
        {
            string newMain = newSection.GetValue("Main", string.Empty);
            string newExtension = newSection.GetValue("Extension", string.Empty);
            string newGame = newSection.GetValue("Game", string.Empty);
            string newMission = newSection.GetValue("Mission", string.Empty);
            string newAi = newSection.GetValue("AI", string.Empty);

            var oldSettings = new IniFile(spawnerSettingsFile.FullName);

            

            var oldSection = oldSettings.GetSection("Settings");

            string oldExtension = string.Empty;
            string oldGame = string.Empty;
            string oldMission = string.Empty;
            string oldAi = string.Empty;

            if (oldSection != null)
            {

                oldExtension = oldSection.GetValue("Extension", string.Empty);
                oldGame = oldSection.GetValue("Game", string.Empty);
                oldMission = oldSection.GetValue("Mission", string.Empty);
                oldAi = oldSection.GetValue("AI", string.Empty);
            }
            

            bool 是否修改()
            {

                if (!oldSettings.SectionExists("Settings")) return true;
                //   string oldMain = oldSection.GetValue("Main", string.Empty);


                if (oldGame != newGame || oldAi != newAi || oldMission != newMission || oldExtension != newExtension) return true;

                if (FilePaths.Count == 0) return true;

                foreach (var fileType in FilePaths)
                {
                    if (!FileHash.TryGetValue(fileType.Key, out var value)) return true;
                    if (string.IsNullOrEmpty(fileType.Value) || !Directory.Exists(fileType.Value)) continue;
                    foreach (var file in Directory.GetFiles(fileType.Value))
                    {
                        if(Path.GetExtension(file) == ".ini") continue;
                        if (!value.TryGetValue(file, out var hash)) return true;

                        var newHash = file.ComputeHash();
                        if (hash != newHash) return true;
                    }
                }

                return false;
            }

            List<string> 获取文件名(string path)
            {
                List<string> files = [];
                if(path == string.Empty || !Directory.Exists(path)) return files;
                foreach (var file in Directory.GetFiles(path))
                {
                    files.Add(Path.GetFileName(file));
                }
                return files;
            }

            if(是否修改())
            {
                try
            {

                    // ProgramConstants.clearCache();
                    // FileHelper.DelFiles(Directory.GetFiles(oldExtension).ToList());
                    if (oldGame + oldMission + oldAi + oldExtension == string.Empty) ProgramConstants.clearCache();
                    else
                    {
                        if(oldGame != string.Empty)
                            FileHelper.DelFiles(获取文件名(oldGame));
                        if(oldMission != string.Empty)
                            FileHelper.DelFiles(获取文件名(oldMission));
                        if (oldAi != string.Empty)
                            FileHelper.DelFiles(获取文件名(oldAi));
                        if (oldExtension != string.Empty)
                        {
                            foreach (var item in oldExtension.Split(','))
                            {
                                if (item.StartsWith("Ares"))
                             
                                    FileHelper.DelFiles(获取文件名($"Mod&AI/Extension/Ares/{item}"));
                                else if (item.StartsWith("Phobos"))
                                    FileHelper.DelFiles(获取文件名($"Mod&AI/Extension/Phobos/{item}"));
                            }
                        }
                    }
                    WindowManager.progress.Report("正在加载游戏文件");
                FileHelper.CopyDirectory(newGame, "./");

                foreach (var extension in newExtension.Split(","))
                {
                    string directoryPath = $"Mod&AI/Extension/{extension}"; // 默认路径
                    if (extension.Contains("Ares"))
                    {
                        // 当extension为"Ares"，Child设置为"Ares3"，否则为extension本身
                       // string extensionChild = extension == "Ares" ? "Ares3" : extension;
                        directoryPath = $"Mod&AI/Extension/Ares/{extension}";
                    }
                    else if (extension.Contains("Phobos"))
                    {
                        // 当extension为"Phobos"，Child设置为"Phobos36"，否则为extension本身
                     //   string extensionChild = extension == "Phobos" ? "Phobos36" : extension;
                        directoryPath = $"Mod&AI/Extension/Phobos/{extension}";
                    }
                    FileHelper.CopyDirectory(directoryPath, "./");
                }

                FileHelper.CopyDirectory(newAi, "./");

                FileHelper.CopyDirectory(newMission, "./");

                FileHelper.CopyDirectory(newMain, "./");

                FilePaths["Game"] = newGame;
                FilePaths["Main"] = newMain;
                FilePaths["Mission"] = newMission;
                FilePaths["AI"] = newAi;


                foreach (var keyValue in FilePaths)
                {
                    if (!FileHash.ContainsKey(keyValue.Key))
                        FileHash.Add(keyValue.Key, []);

                    if (string.IsNullOrEmpty(keyValue.Value) || !Directory.Exists(keyValue.Value)) continue;

                    foreach (var fileName in Directory.GetFiles(keyValue.Value))
                    {
                            if (Path.GetExtension(fileName) == ".ini") continue;
              
                        FileHash[keyValue.Key][fileName] = fileName.ComputeHash();
                        }
                }

                if (!File.Exists($"{newGame}\\thememd.mix"))
                {
                    WindowManager.progress.Report("正在加载音乐");
                    加载音乐();
                }

                WindowManager.progress.Report("正在加载语音");
                FileHelper.CopyDirectory($"Resources/Voice/{UserINISettings.Instance.Voice.Value}", "./");

                    return string.Empty;
            }

            catch (FileLockedException ex)
            {
            //  XNAMessageBox.Show(windowManager, "错误", ex.Message);
              Logger.Log(ex.Message);
                return ex.Message;
            }
                
            }
            return string.Empty;
        }
        private static void Process_Exited(object sender, EventArgs e)
        {
            Process proc = (Process)sender;

            WindowManager.progress.Report(string.Empty);
            Logger.Log("GameProcessLogic: Process exited.");
            UserINISettings.Instance.开始渲染地图?.Invoke();
       
            proc.Exited -= Process_Exited;
            proc.Dispose();
            GameProcessExited?.Invoke();
            获取新的存档();
        }

        public static (List<string>, List<string>) 支持的扩展()
    => (
        Directory.GetDirectories("Mod&AI/Extension/Ares")
                .Select(Path.GetFileName)
                .ToList(),
        Directory.GetDirectories("Mod&AI/Extension/Phobos")
                .Select(Path.GetFileName)
                .ToList()
    );

    }
}