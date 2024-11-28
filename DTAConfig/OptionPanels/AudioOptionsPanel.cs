using System;
using System.Globalization;
using System.IO;
using ClientCore;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.OptionPanels
{
    class AudioOptionsPanel : XNAOptionsPanel
    {
        public AudioOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
            : base(windowManager, iniSettings)
        {
        }

        private XNATrackbar trbScoreVolume;
        private XNATrackbar trbSoundVolume;
        private XNATrackbar trbVoiceVolume;

        private XNALabel lblScoreVolumeValue;
        private XNALabel lblSoundVolumeValue;
        private XNALabel lblVoiceVolumeValue;

        private XNAClientCheckBox chkScoreShuffle;

        private XNALabel lblClientVolumeValue;
        private XNATrackbar trbClientVolume;

        private XNAClientCheckBox chkMainMenuMusic;
        private XNAClientCheckBox chkStopMusicOnMenu;
        private MusicWindow musicWindow;
        private XNAClientDropDown ddVoice;

        public override void Initialize()
        {


            base.Initialize();

            Name = "AudioOptionsPanel";
            // 音乐音量
            var lblScoreVolume = new XNALabel(WindowManager);
            lblScoreVolume.Name = "lblScoreVolume";
            lblScoreVolume.ClientRectangle = new Rectangle(12, 14, 0, 0);
            lblScoreVolume.Text = "Music Volume:".L10N("UI:DTAConfig:MusicVolume");

            lblScoreVolumeValue = new XNALabel(WindowManager);
            lblScoreVolumeValue.Name = "lblScoreVolumeValue";
            lblScoreVolumeValue.FontIndex = 1;
            lblScoreVolumeValue.Text = "10";
            lblScoreVolumeValue.ClientRectangle = new Rectangle(
                Width - lblScoreVolumeValue.Width - 12,
                lblScoreVolume.Y, 0, 0);

            trbScoreVolume = new XNATrackbar(WindowManager);
            trbScoreVolume.Name = "trbScoreVolume";
            trbScoreVolume.ClientRectangle = new Rectangle(
                lblScoreVolume.Right + 16,
                lblScoreVolume.Y - 2,
                lblScoreVolumeValue.X - lblScoreVolume.Right - 31,
                22);
            trbScoreVolume.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            trbScoreVolume.MinValue = 0;
            trbScoreVolume.MaxValue = 10;
            trbScoreVolume.ValueChanged += TrbScoreVolume_ValueChanged;

            // 音效音量
            var lblSoundVolume = new XNALabel(WindowManager);
            lblSoundVolume.Name = "lblSoundVolume";
            lblSoundVolume.ClientRectangle = new Rectangle(lblScoreVolume.X,
                lblScoreVolume.Bottom + 20, 0, 0);
            lblSoundVolume.Text = "Sound Volume:".L10N("UI:DTAConfig:SoundVolume");

            lblSoundVolumeValue = new XNALabel(WindowManager);
            lblSoundVolumeValue.Name = "lblSoundVolumeValue";
            lblSoundVolumeValue.FontIndex = 1;
            lblSoundVolumeValue.Text = "10";
            lblSoundVolumeValue.ClientRectangle = new Rectangle(
                lblScoreVolumeValue.X,
                lblSoundVolume.Y, 0, 0);

            trbSoundVolume = new XNATrackbar(WindowManager);
            trbSoundVolume.Name = "trbSoundVolume";
            trbSoundVolume.ClientRectangle = new Rectangle(
                trbScoreVolume.X,
                lblSoundVolume.Y - 2,
                trbScoreVolume.Width,
                trbScoreVolume.Height);
            trbSoundVolume.BackgroundTexture = trbScoreVolume.BackgroundTexture;
            trbSoundVolume.MinValue = 0;
            trbSoundVolume.MaxValue = 10;
            trbSoundVolume.ValueChanged += TrbSoundVolume_ValueChanged;

            // 语音音量
            var lblVoiceVolume = new XNALabel(WindowManager);
            lblVoiceVolume.Name = "lblVoiceVolume";
            lblVoiceVolume.ClientRectangle = new Rectangle(lblScoreVolume.X,
                lblSoundVolume.Bottom + 20, 0, 0);
            lblVoiceVolume.Text = "Voice Volume:".L10N("UI:DTAConfig:VoiceVolume");

            lblVoiceVolumeValue = new XNALabel(WindowManager);
            lblVoiceVolumeValue.Name = "lblVoiceVolumeValue";
            lblVoiceVolumeValue.FontIndex = 1;
            lblVoiceVolumeValue.Text = "10";
            lblVoiceVolumeValue.ClientRectangle = new Rectangle(
                lblScoreVolumeValue.X,
                lblVoiceVolume.Y, 0, 0);

            trbVoiceVolume = new XNATrackbar(WindowManager);
            trbVoiceVolume.Name = "trbVoiceVolume";
            trbVoiceVolume.ClientRectangle = new Rectangle(
                trbSoundVolume.X,
                lblVoiceVolume.Y - 2,
                trbScoreVolume.Width,
                trbScoreVolume.Height);
            trbVoiceVolume.BackgroundTexture = trbScoreVolume.BackgroundTexture;
            trbVoiceVolume.MinValue = 0;
            trbVoiceVolume.MaxValue = 10;
            trbVoiceVolume.ValueChanged += TrbVoiceVolume_ValueChanged;

            // 客户端音量
            var lblClientVolume = new XNALabel(WindowManager);
            lblClientVolume.Name = "lblClientVolume";
            lblClientVolume.ClientRectangle = new Rectangle(lblVoiceVolume.X,
                trbVoiceVolume.Bottom + 20, 0, 0);
            lblClientVolume.Text = "Client Volume:".L10N("UI:DTAConfig:ClientVolume");

            lblClientVolumeValue = new XNALabel(WindowManager);
            lblClientVolumeValue.Name = "lblClientVolumeValue";
            lblClientVolumeValue.FontIndex = 1;
            lblClientVolumeValue.Text = "0";
            lblClientVolumeValue.ClientRectangle = new Rectangle(
                lblVoiceVolumeValue.X,
                lblClientVolume.Y, 0, 0);

            trbClientVolume = new XNATrackbar(WindowManager);
            trbClientVolume.Name = "trbClientVolume";
            trbClientVolume.ClientRectangle = new Rectangle(
                trbScoreVolume.X,
                lblClientVolume.Y - 2,
                trbScoreVolume.Width,
                trbScoreVolume.Height);
            trbClientVolume.BackgroundTexture = trbScoreVolume.BackgroundTexture;
            trbClientVolume.MinValue = 0;
            trbClientVolume.MaxValue = 10;
            trbClientVolume.ValueChanged += TrbClientVolume_ValueChanged;

            // 随机播放音乐
            chkScoreShuffle = new XNAClientCheckBox(WindowManager);
            chkScoreShuffle.Name = "chkScoreShuffle";
            chkScoreShuffle.ClientRectangle = new Rectangle(
                lblClientVolume.X,
                trbClientVolume.Bottom + 12, 0, 0);
            chkScoreShuffle.Text = "Shuffle Music".L10N("UI:DTAConfig:ShuffleMusic");

            var lblVoice = new XNALabel(WindowManager);
            lblVoice.Name = "lblVoice";
            lblVoice.ClientRectangle = new Rectangle(chkScoreShuffle.X, trbClientVolume.Bottom + 16, 0, 0);
            lblVoice.Text = "Voice:".L10N("UI:Main:Voice");
            AddChild(lblVoice);

            ddVoice = new XNAClientDropDown(WindowManager);
            ddVoice.Name = "ddVoice";
            ddVoice.ClientRectangle = new Rectangle(lblVoice.Right + 12, lblVoice.Top - 2, 160, 20);
            AddChild(ddVoice);

            foreach (string voice in Directory.GetDirectories("Resources/Voice"))
            {
                ddVoice.AddItem(voice);
            }


            // 主菜单音乐
            chkMainMenuMusic = new XNAClientCheckBox(WindowManager);
            chkMainMenuMusic.Name = "chkMainMenuMusic";
            chkMainMenuMusic.ClientRectangle = new Rectangle(
                chkScoreShuffle.X,
                chkScoreShuffle.Bottom + 24, 0, 0);
            chkMainMenuMusic.Text = "Main menu _music".L10N("UI:DTAConfig:MainMenuMusic");
            chkMainMenuMusic.CheckedChanged += ChkMainMenuMusic_CheckedChanged;

            // 大厅中停止播放音乐
            chkStopMusicOnMenu = new XNAClientCheckBox(WindowManager);
            chkStopMusicOnMenu.Name = "chkStopMusicOnMenu";
            chkStopMusicOnMenu.ClientRectangle = new Rectangle(
                chkMainMenuMusic.X, chkMainMenuMusic.Bottom + 24, 0, 0);
            chkStopMusicOnMenu.Text = "Don't play main menu _music in lobbies".L10N("UI:DTAConfig:NoLobbiesMusic");

            musicWindow = new MusicWindow(WindowManager);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, musicWindow);
            musicWindow.Disable();

            var btnMusicWindow = new XNAClientButton(WindowManager)
            {
                Name = "btnMusicWindow",
                X = lblScoreVolume.X,
                Y = chkStopMusicOnMenu.Y + 48,
                Text = "管理游戏内音乐",

            };
            btnMusicWindow.LeftClick += (_, _) => { musicWindow.Enable(); };

           
            AddChild(lblScoreVolume);
            AddChild(lblScoreVolumeValue);
            AddChild(trbScoreVolume);
            AddChild(lblSoundVolume);
            AddChild(lblSoundVolumeValue);
            AddChild(trbSoundVolume);
            AddChild(lblVoiceVolume);
            AddChild(lblVoiceVolumeValue);
            AddChild(trbVoiceVolume);

            AddChild(chkScoreShuffle);

            AddChild(lblClientVolume);
            AddChild(lblClientVolumeValue);
            AddChild(trbClientVolume);

            AddChild(chkMainMenuMusic);
            AddChild(chkStopMusicOnMenu);
            AddChild(btnMusicWindow);

            WindowManager.SoundPlayer.SetVolume(trbClientVolume.Value / 10.0f);
        }

        private EventHandler chkMusicWindow_LeftClick()
        {
            throw new NotImplementedException();
        }

        private void ChkMainMenuMusic_CheckedChanged(object sender, EventArgs e)
        {
            chkStopMusicOnMenu.AllowChecking = chkMainMenuMusic.Checked;
            chkStopMusicOnMenu.Checked = chkMainMenuMusic.Checked;
        }

        private void TrbScoreVolume_ValueChanged(object sender, EventArgs e)
        {
            lblScoreVolumeValue.Text = trbScoreVolume.Value.ToString();
        }

        private void TrbSoundVolume_ValueChanged(object sender, EventArgs e)
        {
            lblSoundVolumeValue.Text = trbSoundVolume.Value.ToString();
        }

        private void TrbVoiceVolume_ValueChanged(object sender, EventArgs e)
        {
            lblVoiceVolumeValue.Text = trbVoiceVolume.Value.ToString();
        }

        private void TrbClientVolume_ValueChanged(object sender, EventArgs e)
        {
            lblClientVolumeValue.Text = trbClientVolume.Value.ToString();
            WindowManager.SoundPlayer.SetVolume(trbClientVolume.Value / 10.0f);
        }

        public override void Load()
        {
            base.Load();

            trbScoreVolume.Value = (int)(IniSettings.ScoreVolume * 10);
            trbSoundVolume.Value = (int)(IniSettings.SoundVolume * 10);
            trbVoiceVolume.Value = (int)(IniSettings.VoiceVolume * 10);

            chkScoreShuffle.Checked = IniSettings.IsScoreShuffle;

            trbClientVolume.Value = (int)(IniSettings.ClientVolume * 10);

            chkMainMenuMusic.Checked = IniSettings.PlayMainMenuMusic;
            chkStopMusicOnMenu.Checked = IniSettings.StopMusicOnMenu;

            ddVoice.SelectedIndex = ddVoice.Items.FindIndex(item => item.Text == UserINISettings.Instance.Voice.Value);
        }

        public override bool Save()
        {
            bool restartRequired = base.Save();

            IniSettings.ScoreVolume.Value = trbScoreVolume.Value / 10.0;
            IniSettings.SoundVolume.Value = trbSoundVolume.Value / 10.0;
            IniSettings.VoiceVolume.Value = trbVoiceVolume.Value / 10.0;

            IniSettings.IsScoreShuffle.Value = chkScoreShuffle.Checked;

            IniSettings.ClientVolume.Value = trbClientVolume.Value / 10.0;

            IniSettings.PlayMainMenuMusic.Value = chkMainMenuMusic.Checked;
            IniSettings.StopMusicOnMenu.Value = chkStopMusicOnMenu.Checked;

            UserINISettings.Instance.Voice.Value = ddVoice.SelectedItem.Text;

            return restartRequired;
        }
    }
}
