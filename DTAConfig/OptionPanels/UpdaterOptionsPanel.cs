using System;
using System.Collections.Generic;
using System.Linq;
using ClientCore;
using ClientCore.Settings;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.OptionPanels
{
    class UpdaterOptionsPanel : XNAOptionsPanel
    {
        private XNAListBox lbUpdateServerList;
        private XNAClientCheckBox chkAutoCheck;
        private XNAClientButton btnForceUpdate;
        private XNALabel lblBeta;
        private XNADropDown ddBeta;

        public event EventHandler OnForceUpdate;

        public UpdaterOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
            : base(windowManager, iniSettings)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            Name = "UpdaterOptionsPanel";

            var lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = "lblDescription";
            lblDescription.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblDescription.Text = ("To change download server priority, select a server from the list and" +
                Environment.NewLine + "use the Move Up / Down buttons to change its priority.").L10N("UI:DTAConfig:ServerPriorityTip");

            lbUpdateServerList = new XNAListBox(WindowManager);
            lbUpdateServerList.Name = "lblUpdateServerList";
            lbUpdateServerList.ClientRectangle = new Rectangle(lblDescription.X, lblDescription.Bottom + 12, Width - 24, 150);
            lbUpdateServerList.LineHeight = 20;
            lbUpdateServerList.TextBorderDistance = 5;
            lbUpdateServerList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            lbUpdateServerList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            var btnMoveUp = new XNAClientButton(WindowManager);
            btnMoveUp.Name = "btnMoveUp";
            btnMoveUp.ClientRectangle = new Rectangle(lbUpdateServerList.X,
                lbUpdateServerList.Bottom + 12, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnMoveUp.Text = "Move Up".L10N("UI:DTAConfig:MoveUp");
            btnMoveUp.LeftClick += btnMoveUp_LeftClick;

            var btnMoveDown = new XNAClientButton(WindowManager);
            btnMoveDown.Name = "btnMoveDown";
            btnMoveDown.ClientRectangle = new Rectangle(
                lbUpdateServerList.Right - UIDesignConstants.BUTTON_WIDTH_133,
                btnMoveUp.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnMoveDown.Text = "Move Down".L10N("UI:DTAConfig:MoveDown");
            btnMoveDown.LeftClick += btnMoveDown_LeftClick;

            chkAutoCheck = new XNAClientCheckBox(WindowManager);
            chkAutoCheck.Name = "chkAutoCheck";
            chkAutoCheck.ClientRectangle = new Rectangle(lblDescription.X,
                btnMoveUp.Bottom + 24, 0, 0);
            chkAutoCheck.Text = "CheckInPut for updates automatically".L10N("UI:DTAConfig:AutoCheckUpdate");
           
            btnForceUpdate = new XNAClientButton(WindowManager);
            btnForceUpdate.Name = "btnForceUpdate";
            btnForceUpdate.ClientRectangle = new Rectangle(btnMoveDown.X, btnMoveDown.Bottom + 24, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnForceUpdate.Text = "Force Update".L10N("UI:DTAConfig:ForceUpdate");
            btnForceUpdate.LeftClick += BtnForceUpdate_LeftClick;

            lblBeta = new XNALabel(WindowManager);
            lblBeta.Name = "lblBeta";
            lblBeta.ClientRectangle = new Rectangle(chkAutoCheck.X, chkAutoCheck.Y + 40, 60, 20);
            lblBeta.Text = "更新通道";

            ddBeta = new XNADropDown(WindowManager);
            ddBeta.Name = "ddBeta";
            ddBeta.ClientRectangle = new Rectangle(lblBeta.X + 80, lblBeta.Y,100,30);
            ddBeta.AddItem("稳定版");
            ddBeta.AddItem("尝鲜版");
            ddBeta.SelectedIndexChanged += DdBeta_SelectedIndexChanged;

            AddChild(lblDescription);
            AddChild(lbUpdateServerList);
            AddChild(btnMoveUp);
            AddChild(btnMoveDown);
            AddChild(chkAutoCheck);
            AddChild(btnForceUpdate);
            AddChild(lblBeta);
            AddChild(ddBeta);
        }

        private void DdBeta_SelectedIndexChanged(object sender, EventArgs e)
        {
            lbUpdateServerList.Items.Clear();
            var keyServers = IniSettings.SettingsIni.GetSectionKeys("DownloadMirrors") ?? [];
            if (keyServers.Count == Updater.ServerMirrors.Count)
            {
                List<string> lstServers = [];
                int nId = 0;
                foreach (var key in keyServers)
                {
                    string strServName = IniSettings.SettingsIni.GetStringValue("DownloadMirrors", key, $"服务器#{nId}");
                    lstServers.Add(strServName);
                    nId++;
                }

                //排序
                try
                {
                    List<ServerMirror> lstServTmp = new List<ServerMirror>();
                    foreach (var strName in lstServers)
                    {
                        ServerMirror serv = Updater.ServerMirrors.First(x => x.Name == strName);
                        if (!string.IsNullOrEmpty(serv.Name))
                            lstServTmp.Add(serv);
                    }
                    Updater.ServerMirrors = lstServTmp;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.Message);
                }
            }

            if (Updater.ServerMirrors != null && Updater.ServerMirrors.Count > 0)
            {
                var lstServers = Updater.ServerMirrors.Where(f => f.Type.Equals(ddBeta.SelectedIndex)).ToList();
                Logger.Log("更新：Update Servers Count: " + lstServers.Count);
                foreach (var updaterServer in lstServers)
                {
                    lbUpdateServerList.AddItem(updaterServer.Name +
                        (!string.IsNullOrEmpty(updaterServer.Location) ?
                        $" ({updaterServer.Location})" : string.Empty));
                }
            }

        }

        private void BtnForceUpdate_LeftClick(object sender, EventArgs e)
        {
            var msgBox = new XNAMessageBox(WindowManager, "Force Update Confirmation".L10N("UI:DTAConfig:ForceUpdateConfirmTitle"),
                    ("WARNING: Force update will result in files being re-verified" + Environment.NewLine +
                    "and re-downloaded. While this may fix problems with game" + Environment.NewLine +
                    "files, this also may delete some custom modifications" + Environment.NewLine +
                    "made to this installation. Use at your own risk!" +
                    Environment.NewLine + Environment.NewLine +
                    "If you proceed, the options window will close and the" + Environment.NewLine +
                    "client will proceed to checking for updates." +
                    Environment.NewLine + Environment.NewLine +
                    "Do you really want to force update?" + Environment.NewLine).L10N("UI:DTAConfig:ForceUpdateConfirmText"), XNAMessageBoxButtons.YesNo);
            msgBox.Show();
            msgBox.YesClickedAction = ForceUpdateMsgBox_YesClicked;
        }

        private void ForceUpdateMsgBox_YesClicked(XNAMessageBox obj)
        {
            Updater.ClearVersionInfo();
            //Updater.GameVersion = "1.5.0.18";
            
            OnForceUpdate?.Invoke(this, EventArgs.Empty);
        }

        private void btnMoveUp_LeftClick(object sender, EventArgs e)
        {
            int selectedIndex = lbUpdateServerList.SelectedIndex;
            if (selectedIndex < 1)
                return;

            var tmp = lbUpdateServerList.Items[selectedIndex - 1];
            lbUpdateServerList.Items[selectedIndex - 1] = lbUpdateServerList.Items[selectedIndex];
            lbUpdateServerList.Items[selectedIndex] = tmp;

            lbUpdateServerList.SelectedIndex--;

            Updater.MoveMirrorUp(ddBeta.SelectedIndex, selectedIndex);
        }

        private void btnMoveDown_LeftClick(object sender, EventArgs e)
        {
            int selectedIndex = lbUpdateServerList.SelectedIndex;
            if (selectedIndex > lbUpdateServerList.Items.Count - 2 || selectedIndex < 0)
                return;

            var tmp = lbUpdateServerList.Items[selectedIndex + 1];
            lbUpdateServerList.Items[selectedIndex + 1] = lbUpdateServerList.Items[selectedIndex];
            lbUpdateServerList.Items[selectedIndex] = tmp;

            lbUpdateServerList.SelectedIndex++;

            Updater.MoveMirrorDown(ddBeta.SelectedIndex, selectedIndex);
        }

        public override void Load()
        {
            base.Load();

            if (Updater.ServerMirrors != null && Updater.ServerMirrors.Count > 0)
            {
                if(IniSettings.Beta != -1)
                    ddBeta.SelectedIndex = IniSettings.Beta.Value;
              
                chkAutoCheck.Checked = IniSettings.CheckForUpdates;
            }
        }

        public override bool Save()
        {
            bool restartRequired = base.Save();

            IniSettings.CheckForUpdates.Value = chkAutoCheck.Checked;

            if (IniSettings.Beta.Value != ddBeta.SelectedIndex)
            {
                restartRequired = true;
                IniSettings.Beta.Value = ddBeta.SelectedIndex;
            }
            IniSettings.SettingsIni.EraseSectionKeys("DownloadMirrors");

            int id = 0;
            if (NetWorkINISettings.Instance != null)
            {
                foreach (ServerMirror um in Updater.ServerMirrors)
                {
                    IniSettings.SettingsIni.SetStringValue("DownloadMirrors", id.ToString(), um.Name);
                    id++;
                }
            }

            //保存服务器列表优先级配置
            NetWorkINISettings.Instance?.SetServerList();
            
            return restartRequired;
        }

        public override void ToggleMainMenuOnlyOptions(bool enable)
        {
            btnForceUpdate.AllowClick = enable;
        }
    }
}
