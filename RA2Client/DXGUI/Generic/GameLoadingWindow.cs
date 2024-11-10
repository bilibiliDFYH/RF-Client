using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using ClientGUI;
using Ra2Client.Domain;
using Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using DTAConfig.Entity;
using DTAConfig;

namespace Ra2Client.DXGUI.Generic
{
    /// <summary>
    /// A window for loading saved singleplayer games.
    /// </summary>
    public class GameLoadingWindow : XNAWindow
    {
        private const string SAVED_GAMES_DIRECTORY = "Saved Games";

        public GameLoadingWindow(WindowManager windowManager, DiscordHandler discordHandler) : base(windowManager)
        {
            this.discordHandler = discordHandler;
        }

        private DiscordHandler discordHandler;

        private XNAMultiColumnListBox lbSaveGameList;
        private XNAClientButton btnLaunch;
        private XNAClientButton btnDelete;
        private XNAClientButton btnCancel;

        private List<SavedGame> savedGames = new List<SavedGame>();

        public override void Initialize()
        {
            Name = "GameLoadingWindow";
            BackgroundTexture = AssetLoader.LoadTexture("loadmissionbg.png");

            ClientRectangle = new Rectangle(0, 0, 600, 380);
            CenterOnParent();

            lbSaveGameList = new XNAMultiColumnListBox(WindowManager);
            lbSaveGameList.Name = nameof(lbSaveGameList);
            lbSaveGameList.ClientRectangle = new Rectangle(13, 13, 574, 317);
            lbSaveGameList.AddColumn("SAVED GAME NAME".L10N("UI:Main:SavedGameNameColumnHeader"), 400);
            lbSaveGameList.AddColumn("DATE / TIME".L10N("UI:Main:SavedGameDateTimeColumnHeader"), 174);
            lbSaveGameList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbSaveGameList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbSaveGameList.SelectedIndexChanged += ListBox_SelectedIndexChanged;
            lbSaveGameList.AllowKeyboardInput = true;

            btnLaunch = new XNAClientButton(WindowManager);
            btnLaunch.Name = nameof(btnLaunch);
            btnLaunch.ClientRectangle = new Rectangle(125, 345, 110, 23);
            btnLaunch.Text = "Load".L10N("UI:Main:ButtonLoad");
            btnLaunch.AllowClick = false;
            btnLaunch.LeftClick += BtnLaunch_LeftClick;

            btnDelete = new XNAClientButton(WindowManager);
            btnDelete.Name = nameof(btnDelete);
            btnDelete.ClientRectangle = new Rectangle(btnLaunch.Right + 10, btnLaunch.Y, 110, 23);
            btnDelete.Text = "Delete".L10N("UI:Main:ButtonDelete");
            btnDelete.AllowClick = false;
            btnDelete.LeftClick += BtnDelete_LeftClick;

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.ClientRectangle = new Rectangle(btnDelete.Right + 10, btnLaunch.Y, 110, 23);
            btnCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;

            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;

            AddChild(lbSaveGameList);
            AddChild(btnLaunch);
            AddChild(btnDelete);
            AddChild(btnCancel);

            base.Initialize();

            ListSaves();
        }

        private void Keyboard_OnKeyPressed(object sender, Rampastring.XNAUI.Input.KeyPressEventArgs e)
        {
            if (Enabled)
            {
                if (e.PressedKey == Keys.Escape)
                {
                    btnCancel.OnLeftClick();
                }
                if (e.PressedKey == Keys.Enter)
                {
                    btnLaunch.OnLeftClick();
                }
            }
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbSaveGameList.SelectedIndex == -1)
            {
                btnLaunch.AllowClick = false;
                btnDelete.AllowClick = false;
            }
            else
            {
                btnLaunch.AllowClick = true;
                btnDelete.AllowClick = true;
            }
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Enabled = false;
        }

        private void BtnLaunch_LeftClick(object sender, EventArgs e)
        {
            SavedGame sg = savedGames[lbSaveGameList.SelectedIndex];
            Logger.Log("Loading saved game " + sg.FileName);

            FileInfo spawnerSettingsFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);

            var spawnIni = new IniFile(spawnerSettingsFile.FullName);

            if (spawnerSettingsFile.Exists)
                spawnerSettingsFile.Delete();

            var saveIni = new IniFile($"{ProgramConstants.GamePath}Saved Games/Save.ini");

            var newMain = saveIni.GetValue(sg.FileName, "Main", string.Empty);
            var newGame = saveIni.GetValue(sg.FileName, "Game", string.Empty);
            var newMission = saveIni.GetValue(sg.FileName, "Mission", string.Empty);
            string newAi = "Mod&AI/AI/Other";
            var newExtension = saveIni.GetValue(sg.FileName, "Extension", string.Empty);
            bool NEW_YR_to_RA2 = saveIni.GetValue(sg.FileName, "YR_to_RA2", false);

            string oldMain = spawnIni.GetValue("Settings", "Main", string.Empty);
            string oldGame = spawnIni.GetValue("Settings", "Game", string.Empty);
            string oldExtension = spawnIni.GetValue("Settings", "Extension", string.Empty);
            bool OLD_YR_to_RA2 = spawnIni.GetValue("Settings", "YR_to_RA2", false);
            string oldMission = spawnIni.GetValue("Settings", "Mission", string.Empty);
            string oldAi = spawnIni.GetValue("Settings", "AI", string.Empty);
            
            try
            {

                if (oldMain != newMain)
                {
                    if (oldMain != string.Empty)
                    {

                        FileHelper.DelFiles(GetDeleteFile(oldMain));

                    }

                    if (newMain != string.Empty)
                        FileHelper.CopyDirectory(newMain, "./");

                }

                //如果和前一次使用的游戏不一样
                if (oldGame != newGame)
                {

                    FileHelper.DelFiles(GetDeleteFile(oldGame));

                    FileHelper.CopyDirectory(newGame, "./");
                }

                if (newExtension != oldExtension)
                {
                    foreach (var extension in oldExtension.Split(","))
                    {
                        string directoryPath = $"Mod&AI/Extension/{extension}"; // 默认路径

                        if (extension.Contains("Ares"))
                        {
                            // 当extension为"Ares"，Child设置为"Ares3"，否则为extension本身
                            string extensionChild = extension == "Ares" ? "Ares3" : extension;
                            directoryPath = $"Mod&AI/Extension/Ares/{extensionChild}";
                        }
                        else if (extension.Contains("Phobos"))
                        {
                            // 当extension为"Phobos"，Child设置为"Phobos36"，否则为extension本身
                            string extensionChild = extension == "Phobos" ? "Phobos36" : extension;
                            directoryPath = $"Mod&AI/Extension/Phobos/{extensionChild}";
                        }

                        // 删除文件操作统一执行
                        FileHelper.DelFiles(GetDeleteFile(directoryPath));
                    }

                    foreach (var extension in newExtension.Split(","))
                    {
                        string directoryPath = $"Mod&AI/Extension/{extension}"; // 默认路径
                        if (extension.Contains("Ares"))
                        {
                            // 当extension为"Ares"，Child设置为"Ares3"，否则为extension本身
                            string extensionChild = extension == "Ares" ? "Ares3" : extension;
                            directoryPath = $"Mod&AI/Extension/Ares/{extensionChild}";
                        }
                        else if (extension.Contains("Phobos"))
                        {
                            // 当extension为"Phobos"，Child设置为"Phobos36"，否则为extension本身
                            string extensionChild = extension == "Phobos" ? "Phobos36" : extension;
                            directoryPath = $"Mod&AI/Extension/Phobos/{extensionChild}";
                        }
                        FileHelper.CopyDirectory(directoryPath, "./");
                    }

                }


                if (oldAi != newAi)
                {

                    FileHelper.DelFiles(GetDeleteFile(oldAi));
                    FileHelper.CopyDirectory(newAi, "./");
                }

                if (oldMission != newMission)
                {
                    FileHelper.DelFiles(GetDeleteFile(oldMission));
                    FileHelper.CopyDirectory(newMission, "./");
                }



            }
            catch (FileLockedException ex)
            {
                XNAMessageBox.Show(WindowManager, "错误", ex.Message);
                return;
            }

            spawnIni = new IniFile(spawnerSettingsFile.FullName);

            var settings = new IniSection("Settings");

            settings.SetValue("Main", newMain);
            //写入新游戏
            settings.SetValue("Game", newGame);
            //写入新扩展
            settings.SetValue("Extension", newExtension);
            //写入新AI
            settings.SetValue("AI", newAi);

            settings.SetValue("Mission", newMission); 

            settings.SetValue("Ra2Mode", newMain == "RA2_Main");

            settings.SetValue("Scenario", "spawnmap.ini");
            settings.SetValue("SaveGameName", sg.FileName);
            settings.SetValue("LoadSaveGame","Yes");
            settings.SetValue("SidebarHack", ClientConfiguration.Instance.SidebarHack);
            settings.SetValue("CustomLoadScreen", LoadingScreenController.GetLoadScreenName("g"));
            settings.SetValue("Firestorm","No");
            settings.SetValue("GameSpeed", UserINISettings.Instance.GameSpeed.Value);
         
            spawnIni.AddSection(settings);
            spawnIni.WriteIniFile();

            FileInfo spawnMapIniFile = SafePath.GetFile(ProgramConstants.GamePath, "spawnmap.ini");

            if (spawnMapIniFile.Exists)
                spawnMapIniFile.Delete();

            using StreamWriter spawnMapStreamWriter = new StreamWriter(spawnMapIniFile.FullName);
            spawnMapStreamWriter.WriteLine("[Map]");
            spawnMapStreamWriter.WriteLine("Size=0,0,50,50");
            spawnMapStreamWriter.WriteLine("LocalSize=0,0,50,50");
            spawnMapStreamWriter.WriteLine();

            discordHandler.UpdatePresence(sg.GUIName, true);

            Enabled = false;
            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;

            GameProcessLogic.StartGameProcess(WindowManager);
        }

        protected List<string> GetDeleteFile(string oldGame)
        {
            if (oldGame == null || oldGame == "")
                return null;

            List<string> deleteFile = new List<string>();

            foreach (string file in Directory.GetFiles(oldGame))
            {
                deleteFile.Add(Path.GetFileName(file));

            }

            return deleteFile;
        }

        private void BtnDelete_LeftClick(object sender, EventArgs e)
        {
            SavedGame sg = savedGames[lbSaveGameList.SelectedIndex];
            var msgBox = new XNAMessageBox(WindowManager, "删除确认".L10N("UI:Main:DeleteConfirmationTitle"),
                string.Format("以下保存的游戏将被永久删除:" + Environment.NewLine +
                    Environment.NewLine +
                    "Filename: {0}" + Environment.NewLine +
                    "Saved game name: {1}" + Environment.NewLine +
                    "Date and time: {2}" + Environment.NewLine +
                    Environment.NewLine +
                    "Are you sure you want to proceed?".L10N("UI:Main:DeleteConfirmationText"),
                    sg.FileName, Renderer.GetSafeString(sg.GUIName, lbSaveGameList.FontIndex), sg.LastModified.ToString()),
                XNAMessageBoxButtons.YesNo);
            msgBox.Show();
            msgBox.YesClickedAction = DeleteMsgBox_YesClicked;
        }

        private void DeleteMsgBox_YesClicked(XNAMessageBox obj)
        {
            SavedGame sg = savedGames[lbSaveGameList.SelectedIndex];

            Logger.Log("Deleting saved game " + sg.FileName);
            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, SAVED_GAMES_DIRECTORY, sg.FileName);
            ListSaves();
        }

        private void GameProcessExited_Callback()
        {
            WindowManager.AddCallback(new Action(GameProcessExited), null);
        }

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;
            discordHandler.UpdatePresence();
        }

        public void ListSaves()
        {
            savedGames.Clear();
            lbSaveGameList.ClearItems();
            lbSaveGameList.SelectedIndex = -1;

            DirectoryInfo savedGamesDirectoryInfo = SafePath.GetDirectory(ProgramConstants.GamePath, SAVED_GAMES_DIRECTORY);

          

            if (!savedGamesDirectoryInfo.Exists)
            {
                Logger.Log("Saved Games directory not found!");
                return;
            }

            IEnumerable<FileInfo> files = savedGamesDirectoryInfo.EnumerateFiles("*.SAV", SearchOption.TopDirectoryOnly);


            foreach (FileInfo file in files)
            {
                ParseSaveGame(file.FullName);
            }

            //DirectoryInfo gamepath = SafePath.GetDirectory(ProgramConstants.GamePath);



            //if (!gamepath.Exists)
            //{
            //    Logger.Log("Saved Games directory not found!");
            //    return;
            //}

            //files = gamepath.EnumerateFiles("*.SAV", SearchOption.TopDirectoryOnly);


            //foreach (FileInfo file in files)
            //{
            //    ParseSaveGame(file.FullName);
            //}

            savedGames = savedGames.OrderBy(sg => sg.LastModified.Ticks).ToList();
            savedGames.Reverse();

            foreach (SavedGame sg in savedGames)
            {
                string[] item = [
                    Renderer.GetSafeString(sg.GUIName, lbSaveGameList.FontIndex),
                    sg.LastModified.ToString() ];
                lbSaveGameList.AddItem(item, true);
            }
        }

        private void ParseSaveGame(string fileName)
        {
            string shortName = Path.GetFileName(fileName);

            SavedGame sg = new SavedGame(shortName);
            if (sg.ParseInfo())
                savedGames.Add(sg);
        }
    }
}
