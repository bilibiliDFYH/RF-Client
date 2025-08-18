using System;
using ClientGUI;
using Localization;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace Ra2Client.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A panel that is used to verify and display map sharing status.
    /// </summary>
    class MapSharingConfirmationPanel : XNAPanel
    {
        public MapSharingConfirmationPanel(WindowManager windowManager) : base(windowManager)
        {
            CnCNetLobby.下载完成 += (_,_) => { btnDownload.Enabled = true; };
        }

        private readonly string MapSharingRequestText = "房主选择了你没有的地图，\n 等等房主点击预览图下方的分享按钮将地图分享给您。";

        private readonly string MapSharingDownloadText =
            "Downloading map...".L10N("UI:Main:MapSharingDownloadText");

        private readonly string MapSharingFailedText =
            ("Downloading map failed. The game host" + Environment.NewLine +
            "needs to change the map or you will be" + Environment.NewLine +
            "unable to participate in the match.").L10N("UI:Main:MapSharingFailedText");

        public event EventHandler MapDownloadConfirmed;

        private XNALabel lblDescription;
        private XNAClientButton btnDownload;

        public override void Initialize()
        {
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.TILED;

            Name = nameof(MapSharingConfirmationPanel);
            //BackgroundTexture = AssetLoader.LoadTexture("msgboxform.png");

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = nameof(lblDescription);
            lblDescription.X = UIDesignConstants.EMPTY_SPACE_SIDES;
            lblDescription.Y = UIDesignConstants.EMPTY_SPACE_TOP;
            lblDescription.Text = MapSharingRequestText;
            AddChild(lblDescription);

            Width = lblDescription.Right + UIDesignConstants.EMPTY_SPACE_SIDES;

            btnDownload = new XNAClientButton(WindowManager);
            btnDownload.Name = nameof(btnDownload);
            btnDownload.Width = UIDesignConstants.BUTTON_WIDTH_92;
            btnDownload.Y = lblDescription.Bottom + UIDesignConstants.EMPTY_SPACE_TOP * 2;
            btnDownload.Visible = false;
            btnDownload.Text = "Download_Notice".L10N("UI:Main:ButtonDownload");
            //下载
            btnDownload.LeftClick += (s, e) => {
                btnDownload.Enabled = false;
                MapDownloadConfirmed?.Invoke(this, EventArgs.Empty);
            };
            AddChild(btnDownload);
            btnDownload.CenterOnParentHorizontally();

            Height = btnDownload.Bottom + UIDesignConstants.EMPTY_SPACE_BOTTOM;

            base.Initialize();

            CenterOnParent();

            Disable();
        }

        public void ShowForMapDownload()
        {
            lblDescription.Text = MapSharingRequestText;
            btnDownload.AllowClick = true;
            Enable();
        }

        public void SetDownloadingStatus()
        {
            lblDescription.Text = MapSharingDownloadText;
            btnDownload.AllowClick = false;
        }

        public void SetFailedStatus()
        {
            lblDescription.Text = MapSharingFailedText;
            btnDownload.AllowClick = false;
        }
    }
}
