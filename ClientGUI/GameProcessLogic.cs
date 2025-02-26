using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ClientCore;
using ClientCore.INIProcessing;
using DTAConfig.Entity;
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

        private static Mod mod;

        /// <summary>
        /// Starts the main game process.
        /// </summary>
        /// 
        public static void StartGameProcess(WindowManager windowManager, IniFile iniFile = null)
        {
            try
            {
                RenderImage.CancelRendering();
                var settings = iniFile.GetSection("Settings");
                string r = 切换文件(settings);

                mod = Mod.Mods.Find(m => m.FilePath == settings.GetValue("Game", string.Empty));


                if (r != string.Empty)
                {
                    if (r == "尤复目录必须为纯净尤复目录")
                    {
                        var guideWindow = new YRPathWindow(windowManager);
                        guideWindow.Show();
                    }
                    else
                        XNAMessageBox.Show(windowManager, "错误", r);
                    return;
                }


                spawnerSettingsFile.Delete();
                iniFile.WriteIniFile(spawnerSettingsFile.FullName);

                if (!File.Exists(Path.Combine(ProgramConstants.游戏目录, "thememd.mix")))
                {
                    WindowManager.progress.Report("正在加载音乐");
                    加载音乐(mod.FilePath);
                }

                FileHelper.CopyDirectory("Saved Games", Path.Combine(ProgramConstants.游戏目录, "Saved Games"));

                var ra2md = Path.Combine(ProgramConstants.游戏目录, mod.SettingsFile);


                if (File.Exists(ra2md))
                {
                    var ra2mdIni = new IniFile(ra2md);
                    IniFile.ConsolidateIniFiles(ra2mdIni, new IniFile("RA2MD.ini"));
                    ra2mdIni.WriteIniFile();
                }
                else
                {
                    File.Copy("RA2MD.ini", ra2md, true);
                }

                File.Copy("spawn.ini", Path.Combine(ProgramConstants.游戏目录, "spawn.ini"), true);

                var keyboardMD = Path.Combine(ProgramConstants.GamePath, "KeyboardMD.ini");
                if (File.Exists(keyboardMD))
                    File.Copy("KeyboardMD.ini", Path.Combine(ProgramConstants.游戏目录, "KeyboardMD.ini"), true);
                if (File.Exists("spawnmap.ini"))
                    File.Copy("spawnmap.ini", Path.Combine(ProgramConstants.游戏目录, "spawnmap.ini"), true);
                // 加载渲染插件
                FileHelper.CopyDirectory(Path.Combine(ProgramConstants.GamePath, "Resources\\Render", UserINISettings.Instance.Renderer.Value), ProgramConstants.游戏目录);

            }
            catch(Exception ex)
            {
                XNAMessageBox.Show(windowManager,"错误",$"出现错误:{ex}");
                return;
            }

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

                if (File.Exists(Path.Combine(ProgramConstants.游戏目录, "syringe.exe")))
                {
                    gameExecutableName = "Syringe.exe";
                    arguments = "\"gamemd.exe\" -SPAWN " + extraCommandLine;
                }
                //else if (File.Exists("NPatch.mix"))
                //{
                //    gameExecutableName = "gamemd-np.exe";
                //    arguments = "-SPAWN " + extraCommandLine;
                //}
                else
                {
                    gameExecutableName = "gamemd-spawn.exe";
                    arguments = "-SPAWN " + extraCommandLine;
                }

                FileInfo gameFileInfo = SafePath.GetFile(ProgramConstants.游戏目录, gameExecutableName);
                if (!File.Exists(gameFileInfo.FullName))
                {
                    XNAMessageBox.Show(windowManager, "错误", $"{gameFileInfo.FullName}不存在，请前往设置清理游戏缓存后重试。");
                    return;
                }

                ProcessStartInfo info = new ProcessStartInfo(gameFileInfo.FullName, arguments)
                {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = ProgramConstants.游戏目录
                };

                var gameProcess = new Process
                {
                    StartInfo = info,
                    EnableRaisingEvents = true,

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
        private static void 加载音乐(string modPath)
        {
            Mix.PackToMix($"{ProgramConstants.GamePath}Resources/thememd/", Path.Combine(ProgramConstants.游戏目录, "thememd.mix"));
            File.Copy($"{ProgramConstants.GamePath}Resources/thememd/thememd.ini", Path.Combine(ProgramConstants.游戏目录, "thememd.ini"),true);
            var csfPath = Path.Combine(modPath, "ra2md.csf");
            if (File.Exists(csfPath))
            {
                var d = new CSF(csfPath).GetCsfDictionary();
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
                    CSF.WriteCSF(d, Path.Combine(ProgramConstants.游戏目录, "ra2md.csf"));
                }

            }
        }
        private static void 获取新的存档()
        {
            var newSaves = Directory.GetFiles($"{ProgramConstants.GamePath}Saved Games");

            if (oldSaves.Length < newSaves.Length)
            {

                var iniFile = new IniFile($"{ProgramConstants.GamePath}Saved Games/Save.ini");
                var spawn = new IniFile(Path.Combine(ProgramConstants.GamePath, "spawn.ini"));
                var game = spawn.GetValue("Settings", "Game", string.Empty);

                var mission = spawn.GetValue("Settings", "Mission", string.Empty);

                var ra2Mode = spawn.GetValue("Settings", "RA2Mode", false);
                var YR_to_RA2 = spawn.GetValue("Settings", "YR_to_RA2", false);
                // 找到在 newSaves 中但不在 oldSaves 中的文件
                var addedFiles = newSaves.Where(newFile => !oldSaves.Contains(newFile)).ToArray();

                foreach (var fileFullPath in addedFiles)
                {
                    string fileName = Path.GetFileName(fileFullPath);

                    iniFile.SetValue(fileName, "Game", game);
                    iniFile.SetValue(fileName, "Mission", mission);
                    iniFile.SetValue(fileName, "RA2Mode", YR_to_RA2);
                }
                iniFile.WriteIniFile();
            }
        }
        public static string 切换文件(IniSection newSection)
        {


            string newGame = newSection.GetValue("Game", string.Empty);
            string newMission = newSection.GetValue("Mission", string.Empty);


            var oldSettings = new IniFile(spawnerSettingsFile.FullName);

            var oldSection = oldSettings.GetSection("Settings");


            string oldGame = string.Empty;
            string oldMission = string.Empty;


            if (oldSection != null)
            {
                oldGame = oldSection.GetValue("Game", string.Empty);
                oldMission = oldSection.GetValue("Mission", string.Empty);
            }


            bool 是否修改()
            {
                if (!Directory.Exists(ProgramConstants.游戏目录)) return true;

                if (!oldSettings.SectionExists("Settings")) return true;
                //   string oldMain = oldSection.GetValue("Main", string.Empty);


                if (oldGame != newGame || oldMission != newMission) return true;

                if (FilePaths.Count == 0) return true;

                foreach (var fileType in FilePaths)
                {
                    if (!FileHash.TryGetValue(fileType.Key, out var value)) return true;
                    if (string.IsNullOrEmpty(fileType.Value) || !Directory.Exists(fileType.Value)) continue;
                    foreach (var file in Directory.GetFiles(fileType.Value))
                    {
                        if (Path.GetExtension(file) == ".ini") continue;
                        if (!value.TryGetValue(file, out var hash)) return true;

                        var newHash = file.ComputeHash();
                        if (hash != newHash) return true;
                    }
                }

                return false;
            }

            if (是否修改())
            {
                try
                {
                    if (Directory.Exists(ProgramConstants.游戏目录))
                        FileHelper.ForceDeleteDirectory(ProgramConstants.游戏目录);

                    if (!ProgramConstants.判断目录是否为纯净尤复(UserINISettings.Instance.YRPath))
                    {
                        return "尤复目录必须为纯净尤复目录";
                    }

                    Directory.CreateDirectory(ProgramConstants.游戏目录);

                    WindowManager.progress.Report("正在加载游戏文件");


                    foreach (var file in ProgramConstants.PureHashes.Keys)
                    {
                        File.Copy(
                            Path.Combine(UserINISettings.Instance.YRPath, Path.GetFileName(file)),
                            Path.Combine(ProgramConstants.游戏目录, Path.GetFileName(file))
                            , true);
                    }

                    if(Directory.Exists("TX"))
                        FileHelper.CopyDirectory("TX", ProgramConstants.游戏目录);
                    if(Directory.Exists("zh"))
                        FileHelper.CopyDirectory("zh", ProgramConstants.游戏目录);


                    File.Copy("gamemd-spawn.exe", Path.Combine(ProgramConstants.游戏目录, "gamemd-spawn.exe"), true);
                    File.Copy("cncnet5.dll", Path.Combine(ProgramConstants.游戏目录, "cncnet5.dll"), true);
                    // 加载模组
                    FileHelper.CopyDirectory(newGame, ProgramConstants.游戏目录);

                    // 加载任务
                    if (newMission != newGame)
                    {

                        var csfs = Directory.GetFiles(newMission, "*.csf")
                                                                    .OrderBy(f => f) // 按文件名升序处理
                                                                    .ToArray();
                        foreach (var csf in csfs)
                        {
                            var tagCsf = csf;
                            if (csf == "ra2.csf")
                            {
                                tagCsf = "ra2md.csf";
                            }
                            if (newMission.Contains("Maps/CP") && UserINISettings.Instance.SimplifiedCSF.Value)
                                CSF.将繁体的CSF转化为简体CSF(csf, Path.Combine(ProgramConstants.游戏目录, tagCsf));
                            else
                                File.Copy(csf, Path.Combine(ProgramConstants.游戏目录, tagCsf), true);
                        }
                    }
                    // 加载战役图
                    if (newSection.KeyExists("CampaignID"))
                    {
                        var 战役临时目录 = SafePath.CombineFilePath(ProgramConstants.GamePath, "Resources\\MissionCache\\");
                        FileHelper.CopyDirectory(战役临时目录, ProgramConstants.游戏目录);
                    }

                    FilePaths["Game"] = newGame;
                    FilePaths["Mission"] = newMission;

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

                    File.Copy("LiteExt.dll", Path.Combine(ProgramConstants.游戏目录, "LiteExt.dll"), true);

                    WindowManager.progress.Report("正在加载语音");
                    FileHelper.CopyDirectory($"Resources/Voice/{UserINISettings.Instance.Voice.Value}", ProgramConstants.游戏目录);

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
            RenderImage.RenderImagesAsync();

            proc.Exited -= Process_Exited;
            proc.Dispose();
            GameProcessExited?.Invoke();
            var keyboardMD = Path.Combine(ProgramConstants.游戏目录, "KeyboardMD.ini");
            if (File.Exists(keyboardMD))
                File.Copy(keyboardMD, "KeyboardMD.ini", true);

            var RA2MD = Path.Combine(ProgramConstants.游戏目录, mod.SettingsFile);
            if (File.Exists(RA2MD))
                File.Copy(RA2MD, "RA2MD.ini", true);
            FileHelper.CopyDirectory(Path.Combine(ProgramConstants.游戏目录, "Saved Games"),"Saved Games");
            获取新的存档();
        }

    }
}