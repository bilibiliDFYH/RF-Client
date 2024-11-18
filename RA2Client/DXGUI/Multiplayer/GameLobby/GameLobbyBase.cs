using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using ClientCore.Enums;
using ClientCore.Statistics;
using ClientGUI;
using Ra2Client.Domain;
using Ra2Client.Domain.Multiplayer;
using Ra2Client.DXGUI.Multiplayer.CnCNet;
using Ra2Client.Online.EventArguments;
using Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using DTAConfig.OptionPanels;
using DTAConfig.Entity;
using Ra2Client.DXGUI.Generic;
using DTAConfig;
using Localization.Tools;
using SixLabors.ImageSharp.Processing;
using Microsoft.VisualBasic;
using System.Threading.Tasks;
using System.Text;
using System.Windows.Forms;

namespace Ra2Client.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A generic base for all game lobbies (Skirmish, LAN and CnCNet).
    /// Contains the common logic for parsing GameOptions and handling player info.
    /// </summary>
    public abstract class GameLobbyBase : INItializableWindow
    {
        protected const int MAX_PLAYER_COUNT = 8;
        protected const int PLAYER_OPTION_VERTICAL_MARGIN = 12;
        protected const int PLAYER_OPTION_HORIZONTAL_MARGIN = 3;
        protected const int PLAYER_OPTION_CAPTION_Y = 6;
        private const int DROP_DOWN_HEIGHT = 21;
        protected readonly string BTN_LAUNCH_GAME = "Launch Game".L10N("UI:Main:ButtonLaunchGame");
        protected readonly string BTN_LAUNCH_READY = "I'm Ready".L10N("UI:Main:ButtonIAmReady");
        protected readonly string BTN_LAUNCH_NOT_READY = "Not Ready".L10N("UI:Main:ButtonNotReady");

        private readonly string FavoriteMapsLabel = "Favorite Maps".L10N("UI:Main:FavoriteMaps");

        private const int RANK_NONE = 0;
        private const int RANK_EASY = 1;
        private const int RANK_MEDIUM = 2;
        private const int RANK_HARD = 3;



        /// <summary>
        /// Creates a new instance of the game lobby base.
        /// </summary>
        /// <param name="windowManager"></param>
        /// <param name="iniName">The name of the lobby in GameOptions.ini.</param>
        /// <param name="mapLoader"></param>
        /// <param name="isMultiplayer"></param>
        /// <param name="discordHandler"></param>
        public GameLobbyBase(
            WindowManager windowManager,
            string iniName,
            MapLoader mapLoader,
            bool isMultiplayer,
            DiscordHandler discordHandler
        ) : base(windowManager)
        {
            _iniSectionName = iniName;
            MapLoader = mapLoader;
            this.isMultiplayer = isMultiplayer;
            this.discordHandler = discordHandler;
        }

        private string _iniSectionName;

        protected XNAPanel PlayerOptionsPanel;

        protected List<MultiplayerColor> MPColors;

        public List<GameLobbyCheckBox> CheckBoxes = new List<GameLobbyCheckBox>();
        public List<GameLobbyDropDown> DropDowns = new List<GameLobbyDropDown>();

        private ModManager _modManager;

        protected DiscordHandler discordHandler;

        protected MapLoader MapLoader;
        /// <summary>
        /// The list of multiplayer game mode maps.
        /// Each is an instance of a map for a specific game mode.
        /// </summary>
        protected GameModeMapCollection GameModeMaps => MapLoader.GameModeMaps;

        protected GameModeMapFilter gameModeMapFilter;

        private GameModeMap _gameModeMap;

        /// <summary>
        /// The currently selected game mode.
        /// </summary>
        protected GameModeMap GameModeMap
        {
            get => _gameModeMap;
            set
            {
                var oldGameModeMap = _gameModeMap;
                _gameModeMap = value;
                if (value != null && oldGameModeMap != value)
                    UpdateDiscordPresence();
            }
        }

        protected Map Map => GameModeMap?.Map;
        protected GameMode GameMode => GameModeMap?.GameMode;

        protected XNAClientDropDown[] ddPlayerNames;
        protected XNAClientDropDown[] ddPlayerSides;
        protected XNAClientDropDown[] ddPlayerColors;
        protected XNAClientDropDown[] ddPlayerStarts;
        protected XNAClientDropDown[] ddPlayerTeams;

        protected XNAClientButton btnPlayerExtraOptionsOpen;
        protected PlayerExtraOptionsPanel PlayerExtraOptionsPanel;

        protected XNAClientButton btnLeaveGame;
        protected GameLaunchButton btnLaunchGame;

        protected XNAClientButton btnPickRandomMap;
        protected XNAClientButton btnAginLoadMaps;
        protected XNAClientButton btnRandomMap;


        protected XNALabel lblMapName;
        protected XNALabel lblMapAuthor;
        protected XNALabel lblGameMode;
        protected XNALabel lblMapSize;

        protected XNALabel lblscreen;
        protected XNADropDown ddPeople;

        protected GameLobbyDropDown cmbGame;
        protected GameLobbyDropDown cmbAI;

        protected XNAClientCheckBox chkExtension;

        protected MapPreviewBox MapPreviewBox;

        protected int count = 0;

        protected Rectangle MapPreviewBoxPosition;

        protected XNAMultiColumnListBox lbGameModeMapList;
        protected XNAClientDropDown ddGameModeMapFilter;
        protected XNALabel lblGameModeSelect;
        private XNAContextMenu ModMenu; //mod选择器右击菜单
        //游戏模式介绍
        protected XNALabel lblModeText;

        private 生成随机地图窗口 randomMap;

        protected XNAContextMenu mapContextMenu;
        private XNAContextMenuItem toggleFavoriteItem;

        protected XNAClientStateButton<SortDirection> btnMapSortAlphabetically;

        protected XNASuggestionTextBox tbMapSearch;

        protected List<PlayerInfo> Players = new List<PlayerInfo>();
        protected List<PlayerInfo> AIPlayers = new List<PlayerInfo>();

        protected virtual PlayerInfo FindLocalPlayer() => Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

        protected bool PlayerUpdatingInProgress { get; set; }

        protected Texture2D[] RankTextures;

        /// <summary>
        /// The seed used for randomizing player options.
        /// </summary>
        protected int RandomSeed { get; set; }

        /// <summary>
        /// An unique identifier for this game.
        /// </summary>
        protected int UniqueGameID { get; set; }
        protected int SideCount { get; private set; }
        protected int RandomSelectorCount { get; private set; } = 1;

        protected List<int[]> RandomSelectors = new List<int[]>();

        private readonly bool isMultiplayer = false;

        private MatchStatistics matchStatistics;

        private bool disableGameOptionUpdateBroadcast = false;

        protected EventHandler<MultiplayerNameRightClickedEventArgs> MultiplayerNameRightClicked;

        /// <summary>
        /// If set, the client will remove all starting waypoints from the map
        /// before launching it.
        /// </summary>
        protected bool RemoveStartingLocations { get; set; } = false;
        protected IniFile GameOptionsIni { get; private set; }

        protected XNAClientButton BtnSaveLoadGameOptions { get; set; }
        private XNAContextMenu loadSaveGameOptionsMenu { get; set; }

        private LoadOrSaveGameOptionPresetWindow loadOrSaveGameOptionPresetWindow;

        private string[] oldSaves;

        public override void Initialize()
        {


            Name = _iniSectionName;
            ClientRectangle = new Rectangle(0, 0, 1280, 768);
            WindowManager.CenterControlOnScreen(this);
            BackgroundTexture = AssetLoader.LoadTexture("gamelobbybg.png");

            RankTextures = new Texture2D[4]
            {
                AssetLoader.LoadTexture("rankNone.png"),
                AssetLoader.LoadTexture("rankEasy.png"),
                AssetLoader.LoadTexture("rankNormal.png"),
                AssetLoader.LoadTexture("rankHard.png")
            };

            MPColors = MultiplayerColor.LoadColors();

            GameOptionsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), "GameOptions.ini"));

            base.Initialize();

            _modManager = ModManager.GetInstance(WindowManager);
            _modManager.MyEvent += (_, _) => ReloadMod();
            PlayerOptionsPanel = FindChild<XNAPanel>(nameof(PlayerOptionsPanel));

            btnLeaveGame = FindChild<XNAClientButton>(nameof(btnLeaveGame));

            btnLeaveGame.Text = "Leave Game".L10N("UI:Main:LeaveGame");

            btnLeaveGame.LeftClick += BtnLeaveGame_LeftClick;

            MapPreviewBox = FindChild<MapPreviewBox>("MapPreviewBox");

            MapPreviewBoxPosition = MapPreviewBox.ClientRectangle;


            MapPreviewBox.SetFields(Players, AIPlayers, MPColors, GameOptionsIni.GetStringValue("General", "Sides", String.Empty).Split(','), GameOptionsIni);
            
            MapPreviewBox.ToggleFavorite += MapPreviewBox_ToggleFavorite;

            MapPreviewBox.LeftClick += MapPreviewBox_LeftClick;

            btnLaunchGame = FindChild<GameLaunchButton>(nameof(btnLaunchGame));
            btnLaunchGame.LeftClick += BtnLaunchGame_LeftClick;
            btnLaunchGame.InitStarDisplay(RankTextures);

            lblMapName = FindChild<XNALabel>(nameof(lblMapName));
            lblMapAuthor = FindChild<XNALabel>(nameof(lblMapAuthor));
            lblGameMode = FindChild<XNALabel>(nameof(lblGameMode));
            lblMapSize = FindChild<XNALabel>(nameof(lblMapSize));

            lbGameModeMapList = FindChild<XNAMultiColumnListBox>("lbMapList"); // lbMapList for backwards compatibility
            lbGameModeMapList.SelectedIndexChanged += LbGameModeMapList_SelectedIndexChanged;
            lbGameModeMapList.RightClick += LbGameModeMapList_RightClick;
            lbGameModeMapList.AllowKeyboardInput = true; //!isMultiplayer
            lbGameModeMapList.LineHeight = 25; //行间距扩大
            lbGameModeMapList.FontIndex = 1;

            UserINISettings.Instance.ReLoadMissionList += () => btnAginLoadMaps_LeftClick(null, null);

            ModMenu = new XNAContextMenu(WindowManager);
            ModMenu.Name = nameof(ModMenu);
            ModMenu.Width = 100;
            ModMenu.AddItem(new XNAContextMenuItem
            {
                Text = "模组管理器",
                SelectAction = ModManagerEnabled
            });
            ModMenu.AddItem(new XNAContextMenuItem
            {
                Text = "导入Mod",
                SelectAction = () =>
                {
                    ModManagerEnabled();
                    _modManager.BtnNew.OnLeftClick();
                }
            });
            AddChild(ModMenu);

            chkExtension = FindChild<XNAClientCheckBox>(nameof(chkExtension));
            chkExtension.CheckedChanged += ChkExtension_SelectedChanged;
            chkExtension.LeftClick += (_, _) =>
            {
                if (UserINISettings.Instance.ChkExtensionIsFirst.Value)
                {
                    XNAMessageBox.Show(WindowManager, "扩展平台", "打开后会启用扩展平台(如Ares,Phobos).\n" +
                        "部分功能只有在开启扩展平台后才能使用。如shift连点器，部分皮肤（建造预览，飞机尾迹等等），同时也会修复一些原版的bug（特性）。" +
                        "\n然而有些地图或任务使用了原版的特性，(如林德拉克计划)必须不开启扩展才能正常游玩。");

                    UserINISettings.Instance.ChkExtensionIsFirst.Value = false;
                    UserINISettings.Instance.SaveSettings();
                }
            };
            cmbGame = FindChild<GameLobbyDropDown>(nameof(cmbGame));

            cmbGame.SelectedIndexChanged += CmbGame_SelectedChanged;

            cmbGame.RightClick += (s, e) => ModMenu.Open(GetCursorPoint());

            cmbAI = FindChild<GameLobbyDropDown>(nameof(cmbAI));
            cmbAI.SelectedIndexChanged += CmbAI_SelectedChanged;
          

            mapContextMenu = new XNAContextMenu(WindowManager);
            mapContextMenu.Name = nameof(mapContextMenu);
            mapContextMenu.Width = 100;
            mapContextMenu.AddItem("Delete Map".L10N("UI:Main:DeleteMap"), DeleteMapConfirmation, null, CanDeleteMap);
            toggleFavoriteItem = new XNAContextMenuItem
            {
                Text = "Favorite".L10N("UI:Main:Favorite"),
                SelectAction = ToggleFavoriteMap
            };
            mapContextMenu.AddItem(toggleFavoriteItem);
            //mapContextMenu.AddItem(new XNAContextMenuItem
            //{
            //    Text = "添加至其他游戏模式",
            //    SelectAction = 添加至其他游戏模式
            //});
            //AddChild(mapContextMenu);

            mapContextMenu.AddItem(new XNAContextMenuItem
            {
                Text = "删除重复地图",
                SelectAction = 删除重复地图
            });
            AddChild(mapContextMenu);

            XNAPanel rankHeader = new XNAPanel(WindowManager);
            rankHeader.BackgroundTexture = AssetLoader.LoadTexture("rank.png");
            rankHeader.ClientRectangle = new Rectangle(0, 0, rankHeader.BackgroundTexture.Width,
                20);

            XNAListBox rankListBox = new XNAListBox(WindowManager);
            rankListBox.TextBorderDistance = 2;

            lbGameModeMapList.AddColumn(rankHeader, rankListBox);
            lbGameModeMapList.AddColumn("MAP NAME".L10N("UI:Main:MapNameHeader"), lbGameModeMapList.Width - RankTextures[1].Width - 3);

            ddGameModeMapFilter = FindChild<XNAClientDropDown>("ddGameMode"); // ddGameMode for backwards compatibility
            ddGameModeMapFilter.SelectedIndexChanged += DdGameModeMapFilter_SelectedIndexChanged;

            ddGameModeMapFilter.AddItem(CreateGameFilterItem(FavoriteMapsLabel, new GameModeMapFilter(GetFavoriteGameModeMaps)));

            foreach (GameMode gm in GameModeMaps.GameModes)
                ddGameModeMapFilter.AddItem(CreateGameFilterItem(gm.UIName.L10N("UI:GameMode:" + gm.Name), new GameModeMapFilter(GetGameModeMaps(gm))));

            lblGameModeSelect = FindChild<XNALabel>(nameof(lblGameModeSelect));
            // lblModeText = FindChild<XNALabel>(nameof(lblModeText));

            InitBtnMapSort();

            tbMapSearch = FindChild<XNASuggestionTextBox>(nameof(tbMapSearch));
            tbMapSearch.InputReceived += TbMapSearch_InputReceived;
            tbMapSearch.Suggestion = "Search map...".L10N("UI:Main:SearchMapTip");

            btnPickRandomMap = FindChild<XNAClientButton>(nameof(btnPickRandomMap));
            btnPickRandomMap.LeftClick += BtnPickRandomMap_LeftClick;

            btnAginLoadMaps = new XNAClientButton(WindowManager);
            //   btnAginLoadMaps = FindChild<XNAClientButton>(nameof(btnAginLoadMaps));
            btnAginLoadMaps.IdleTexture = AssetLoader.LoadTexture("133pxtab.png");
            btnAginLoadMaps.HoverTexture = AssetLoader.LoadTexture("133pxtab_c.png");
            btnAginLoadMaps.Text = "Refresh the map".L10N("UI:Main:AginLoad");
            btnAginLoadMaps.ClientRectangle = new Rectangle(btnLaunchGame.X, lbGameModeMapList.Y - 35, btnLaunchGame.Width - 20, btnLaunchGame.Height);
            btnAginLoadMaps.LeftClick += btnAginLoadMaps_LeftClick;
            AddChild(btnAginLoadMaps);

            lblscreen = new XNALabel(WindowManager);
            lblscreen.Name = nameof(lblscreen);
            lblscreen.Text = "人数：";
            lblscreen.ClientRectangle = new Rectangle(btnAginLoadMaps.X, ddGameModeMapFilter.Y, 0, 0);
            AddChild(lblscreen);

            ddPeople = new XNADropDown(WindowManager);
            ddPeople.Name = nameof(ddPeople);
            ddPeople.ClientRectangle = new Rectangle(lblscreen.X + 50 , ddGameModeMapFilter.Y, 60, 25);
            AddChild(ddPeople);

            ddPeople.AddItem("-");

            for (int i = 2; i <= 8; i++)
            {
                ddPeople.AddItem(i.ToString());
            }

            ddPeople.SelectedIndex = 0;
            ddPeople.SelectedIndexChanged += DdPeople_SelectedIndexChanged;

            lblModeText = new XNALabel(WindowManager);
            lblModeText.Name = nameof(lblModeText);
            lblModeText.ClientRectangle = new Rectangle(btnAginLoadMaps.X + 120, btnAginLoadMaps.Y + 5 , 0, 0);
            AddChild(lblModeText);

            randomMap = new 生成随机地图窗口(WindowManager, MapLoader);
            // randomMap.ClientRectangle = new Rectangle(200, 100, 800, 500);
            AddAndInitializeWithControl(WindowManager, randomMap);
            randomMap.Disable();
            randomMap.EnabledChanged += randomMap_EnabledChanged;

            btnRandomMap = new XNAClientButton(WindowManager);
            //  btnRandomMap = FindChild<XNAClientButton>(nameof(btnRandomMap));
            btnRandomMap.IdleTexture = AssetLoader.LoadTexture("133pxtab.png");
            btnRandomMap.HoverTexture = AssetLoader.LoadTexture("133pxtab_c.png");
            btnRandomMap.Text = "Generate map".L10N("UI:Main:RanMap");
            btnRandomMap.Disable();
            btnRandomMap.ClientRectangle = new Rectangle(btnLaunchGame.X + 150, btnLaunchGame.Y, btnLaunchGame.Width, btnLaunchGame.Height);
            btnRandomMap.LeftClick += (sender, s) => randomMap.Enable();
            AddChild(btnRandomMap);
            CheckBoxes.ForEach(chk => chk.CheckedChanged += ChkBox_CheckedChanged);
            DropDowns.ForEach(dd => dd.SelectedIndexChanged += Dropdown_SelectedIndexChanged);

            UserINISettings.Instance.继续渲染地图 += MapLoader.ResumeRendering;
            UserINISettings.Instance.暂停渲染地图 += MapLoader.PauseRendering;

            RemoveChild(MapPreviewBox);

            AddChild(MapPreviewBox);
            InitializeGameOptionPresetUI();

            CmbGame_SelectedChanged(cmbGame, null);

           // chkExtension.Checked = true;

            //foreach (var item in Children)
            //{
            //    item.Visible = false;
            //}

        }

        private void 删除重复地图()
        {
            var duplicateFiles = MapLoader.检测重复地图();

            // 如果有重复文件，弹出提示框
            if (duplicateFiles.Count != 0)
            {
                var box = new XNAMessageBox(WindowManager, "确认删除", $"检测到以下重复的地图，是否删除？\n{string.Join('\n', [.. duplicateFiles])}", XNAMessageBoxButtons.YesNo);

                box.YesClickedAction += (_) =>
                {
                    try
                    {
                        duplicateFiles.ForEach(file => File.Delete(file));
                        btnAginLoadMaps_LeftClick(null, null);
                    }
                    catch(Exception ex)
                    {
                        XNAMessageBox.Show(WindowManager, "错误", $"删除文件遇到错误\n{ex}");
                    }

                };

                box.Show();
            }
            else
            {
                XNAMessageBox.Show(WindowManager,"信息", "没有找到重复的地图");
            }
        }

        private void 添加至其他游戏模式()
        {
            if (Map == null) return;
          
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, new 添加至游戏模式(WindowManager, Map, GameModeMaps));
        }

        private void CmbAI_SelectedChanged(object sender, EventArgs e)
        {
            if (cmbAI.SelectedItem == null)
            {
                cmbAI.SelectedIndex = 0;
                return;
            }

            if (((AI)cmbAI.SelectedItem.Tag).ExtensionOn)
            {
                chkExtension.Checked = true;
                chkExtension.AllowChecking = false;
            }
            else
            {
                chkExtension.AllowChecking = true;
            }

        }

        private void ChkExtension_SelectedChanged(object sender, EventArgs e)
        {
           

            ExtensionChange();

            //if (ddPlayerColors != null)
            //{
            //    string randomColor = GameOptionsIni.GetStringValue("General", "RandomColor", "255,255,255");
            //    int colorRange = chkExtension.Checked ? 16 : 8;

            //    foreach (var dd in ddPlayerColors)
            //    {
            //        dd.Items.Clear();
            //        dd.AddItem("Random".L10N("UI:Main:Random"), AssetLoader.GetColorFromString(randomColor));

            //        foreach (var mpColor in MPColors.Take(colorRange))
            //        {
            //            dd.AddItem(mpColor.Name, mpColor.TextColor);
            //        }

            //        if (dd.SelectedItem == null) dd.SelectedIndex = 0;
            //    }
            //}
        }

        public void ExtensionChange()
        {

            if (cmbGame.SelectedItem == null) return;

            Mod mod = cmbGame.SelectedItem.Tag as Mod;

            foreach (var chk in CheckBoxes)
            {
                if (chkExtension.Checked)
                {
                    foreach (var Extension in chk.Extension.Split(','))
                    {

                        if (mod.Extension.LastIndexOf(Extension) == -1)
                        {
                            chk.Checked = false;
                            chk.AllowChecking = false;
                            break;
                        }
                        chk.AllowChecking = true;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(chk.Extension))
                    {
                        chk.Checked = false;
                        chk.AllowChecking = false;
                    }
                    else
                    {
                        chk.AllowChecking = true;
                    }
                }
            }

            foreach (var dd in DropDowns)
            {
                if (chkExtension.Checked)
                {
                    foreach (var Extension in dd.Extension.Split(','))
                    {

                        if (mod.Extension.LastIndexOf(Extension) == -1)
                        {

                            dd.SelectedIndex = dd.defaultIndex;
                            dd.AllowDropDown = false;
                            break;
                        }
                        
                        dd.AllowDropDown = true;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(dd.Extension))
                    {
                        dd.SelectedIndex = dd.defaultIndex;
                        dd.AllowDropDown = false;
                    }
                    else
                    {
                        dd.AllowDropDown = true;
                    }
                }
            }


        }

        protected virtual void ModManagerEnabled()
        {
            var modManager = ModManager.GetInstance(WindowManager);
            if (modManager.Enabled)
                return;
            var dp = DarkeningPanel.AddAndInitializeWithControl(WindowManager, modManager);

            modManager.DDModAI.SelectedIndex = 0;
            modManager.Enable();
            modManager.EnabledChanged += (_, _) =>
            {
                DarkeningPanel.RemoveControl(dp, WindowManager, modManager);
            };
        }

        private void CmbGame_SelectedChanged(object sender, EventArgs e)
        {
            Mod mod = ((Mod)cmbGame.SelectedItem.Tag);
            #region 处理扩展按钮兼容
            ExtensionChange();
            if (mod.ExtensionOn) //必须使用扩展
            {
                chkExtension.Checked = true;
                chkExtension.AllowChecking = false;
            }
            else
            {
                chkExtension.AllowChecking = true;
            }

            if (mod.Extension == string.Empty) // 没有可用扩展
            {
                chkExtension.Checked = false;
                chkExtension.AllowChecking = false;
            }
            else
            {
                chkExtension.AllowChecking = true;
            }
            #endregion

            #region 处理AI
            if (cmbAI != null && cmbAI.SelectedItem != null)
            {
                cmbAI.Items.Clear();
                foreach (var ai in AI.AIs)
                {
                    if (!mod.UseAI.Contains(ai.ID, StringComparison.CurrentCulture))
                        continue;
                    cmbAI.AddItem(new XNADropDownItem() { Text = ai.Name, Tag = ai });
                }
            }
            #endregion

        }

        private void MapPreviewBox_LeftClick(object sender, EventArgs e)
        {

            if (count % 2 == 0)

                MapPreviewBox.ClientRectangle = new Rectangle(0, 0, 1280, 768);
            else

                MapPreviewBox.ClientRectangle = MapPreviewBoxPosition;

            count++;

            base.OnLeftClick();

        }
        private void DdPeople_SelectedIndexChanged(object sender, EventArgs e)
        {
            gameModeMapFilter = new GameModeMapFilter(GetPeopleGameModeMaps(ddGameModeMapFilter.SelectedItem.Text, ddPeople.SelectedIndex));
               
            ListMaps();

        }

        /// <summary>
        /// Until the GUICreator can handle typed classes, this must remain manually done.
        /// </summary>
        private void InitBtnMapSort()
        {
            btnMapSortAlphabetically = new XNAClientStateButton<SortDirection>(WindowManager, new Dictionary<SortDirection, Texture2D>()
            {
                { SortDirection.None, AssetLoader.LoadTexture("sortAlphaNone.png") },
                { SortDirection.Asc, AssetLoader.LoadTexture("sortAlphaAsc.png") },
                { SortDirection.Desc, AssetLoader.LoadTexture("sortAlphaDesc.png") },
            });
            btnMapSortAlphabetically.Name = nameof(btnMapSortAlphabetically);
            btnMapSortAlphabetically.ClientRectangle = new Rectangle(
                ddGameModeMapFilter.X + -ddGameModeMapFilter.Height - 4, ddGameModeMapFilter.Y,
                ddGameModeMapFilter.Height, ddGameModeMapFilter.Height
            );
            btnMapSortAlphabetically.LeftClick += BtnMapSortAlphabetically_LeftClick;
            btnMapSortAlphabetically.SetToolTipText("Sort Maps Alphabetically".L10N("UI:Main:MapSortAlphabeticallyToolTip"));
            RefreshMapSortAlphabeticallyBtn();
            AddChild(btnMapSortAlphabetically);

            // Allow repositioning / disabling in INI.
            ReadINIForControl(btnMapSortAlphabetically);
        }

        private void InitializeGameOptionPresetUI()
        {
            BtnSaveLoadGameOptions = FindChild<XNAClientButton>(nameof(BtnSaveLoadGameOptions), true);

            if (BtnSaveLoadGameOptions != null)
            {
                loadOrSaveGameOptionPresetWindow = new LoadOrSaveGameOptionPresetWindow(WindowManager);
                loadOrSaveGameOptionPresetWindow.Name = nameof(loadOrSaveGameOptionPresetWindow);
                loadOrSaveGameOptionPresetWindow.PresetLoaded += (sender, s) => HandleGameOptionPresetLoadCommand(s);
                loadOrSaveGameOptionPresetWindow.PresetSaved += (sender, s) => HandleGameOptionPresetSaveCommand(s);
                loadOrSaveGameOptionPresetWindow.Disable();
                var loadConfigMenuItem = new XNAContextMenuItem()
                {
                    Text = "Load".L10N("UI:Main:ButtonLoad"),
                    SelectAction = () => loadOrSaveGameOptionPresetWindow.Show(true)
                };
                var saveConfigMenuItem = new XNAContextMenuItem()
                {
                    Text = "Save".L10N("UI:Main:ButtonSave"),
                    SelectAction = () => loadOrSaveGameOptionPresetWindow.Show(false)
                };

                loadSaveGameOptionsMenu = new XNAContextMenu(WindowManager);
                loadSaveGameOptionsMenu.Name = nameof(loadSaveGameOptionsMenu);
                loadSaveGameOptionsMenu.ClientRectangle = new Rectangle(0, 0, 75, 0);
                loadSaveGameOptionsMenu.Items.Add(loadConfigMenuItem);
                loadSaveGameOptionsMenu.Items.Add(saveConfigMenuItem);

                BtnSaveLoadGameOptions.LeftClick += (sender, args) =>
                    loadSaveGameOptionsMenu.Open(GetCursorPoint());

                AddChild(loadSaveGameOptionsMenu);
                AddChild(loadOrSaveGameOptionPresetWindow);
            }
        }

        public static void AddAndInitializeWithControl(WindowManager wm, XNAControl control)
        {
            var dp = new DarkeningPanel(wm);
            wm.AddAndInitializeControl(dp);
            dp.AddChild(control);
        }

        private void BtnMapSortAlphabetically_LeftClick(object sender, EventArgs e)
        {
            UserINISettings.Instance.MapSortState.Value = (int)btnMapSortAlphabetically.GetState();

            RefreshMapSortAlphabeticallyBtn();
            UserINISettings.Instance.SaveSettings();
            ListMaps();
        }

        private void RefreshMapSortAlphabeticallyBtn()
        {
            if (Enum.IsDefined(typeof(SortDirection), UserINISettings.Instance.MapSortState.Value))
                btnMapSortAlphabetically.SetState((SortDirection)UserINISettings.Instance.MapSortState.Value);
        }

        private static XNADropDownItem CreateGameFilterItem(string text, GameModeMapFilter filter)
        {
            return new XNADropDownItem
            {
                Text = text,
                Tag = filter
            };
        }

        protected bool IsFavoriteMapsSelected() => ddGameModeMapFilter.SelectedItem?.Text == FavoriteMapsLabel;

        private List<GameModeMap> GetFavoriteGameModeMaps() =>
            GameModeMaps.Where(gmm => gmm.IsFavorite).ToList();

        private Func<List<GameModeMap>> GetGameModeMaps(GameMode gm) => () =>
            GameModeMaps.Where(gmm => gmm.GameMode == gm).ToList();

        private Func<List<GameModeMap>> GetPeopleGameModeMaps(string gm, int i) => () =>
            [.. GameModeMaps.Where(gmm => gmm.Map!=null && gmm.GameMode!=null && (gmm.Map.MaxPlayers == (i+1) || i == 0) && (gmm.GameMode.UIName == gm || (gmm.IsFavorite && gm == "最爱的地图")))];

        private void RefreshBtnPlayerExtraOptionsOpenTexture()
        {
            if (btnPlayerExtraOptionsOpen != null)
            {
                var textureName = GetPlayerExtraOptions().IsDefault() ? "optionsButton.png" : "optionsButtonActive.png";
                var hoverTextureName = GetPlayerExtraOptions().IsDefault() ? "optionsButton_c.png" : "optionsButtonActive_c.png";
                var hoverTexture = AssetLoader.AssetExists(hoverTextureName) ? AssetLoader.LoadTexture(hoverTextureName) : null;
                btnPlayerExtraOptionsOpen.IdleTexture = AssetLoader.LoadTexture(textureName);
                btnPlayerExtraOptionsOpen.HoverTexture = hoverTexture;
            }
        }

        protected void HandleGameOptionPresetSaveCommand(GameOptionPresetEventArgs e) => HandleGameOptionPresetSaveCommand(e.PresetName);

        protected void HandleGameOptionPresetSaveCommand(string presetName)
        {
            string error = AddGameOptionPreset(presetName);
            if (!string.IsNullOrEmpty(error))
                AddNotice(error);
        }

        protected void HandleGameOptionPresetLoadCommand(GameOptionPresetEventArgs e) => HandleGameOptionPresetLoadCommand(e.PresetName);

        protected void HandleGameOptionPresetLoadCommand(string presetName)
        {
            if (LoadGameOptionPreset(presetName))
                AddNotice("Game option preset loaded succesfully.".L10N("UI:Main:PresetLoaded"));
            else
                AddNotice(string.Format("Preset {0} not found!".L10N("UI:Main:PresetNotFound"), presetName));
        }

        protected void AddNotice(string message) => AddNotice(message, Color.White);

        protected abstract void AddNotice(string message, Color color);

        private void BtnPickRandomMap_LeftClick(object sender, EventArgs e) => PickRandomMap();

        public void btnAginLoadMaps_LeftClick(object sender, EventArgs e)
        {
            MapLoader.AgainLoadMaps();

            ddGameModeMapFilter.Items.Clear();

            ddGameModeMapFilter.AddItem(CreateGameFilterItem(FavoriteMapsLabel, new GameModeMapFilter(GetFavoriteGameModeMaps)));
            foreach (GameMode gm in GameModeMaps.GameModes)
            {
                ddGameModeMapFilter.AddItem(CreateGameFilterItem(gm.UIName.L10N("UI:GameMode:" + gm.Name), new GameModeMapFilter(GetGameModeMaps(gm))));

            }

            MapPreviewBox.UpdateMap();
            int i = ddGameModeMapFilter.SelectedIndex;
            ddGameModeMapFilter.SelectedIndex = 0;
            ddGameModeMapFilter.SelectedIndex = i;

        }

        private void randomMap_EnabledChanged(object sender, EventArgs e)
        {
            var r = Path.GetFileNameWithoutExtension(randomMap.GetIsSave());

            if (!randomMap.Enabled && !string.IsNullOrEmpty(r))
            {

                btnAginLoadMaps_LeftClick(null,null);

                ddGameModeMapFilter.SelectedIndex = ddGameModeMapFilter.Items.FindIndex(d => d.Text == "常规作战");

                for (int i = 0; i < lbGameModeMapList.ItemCount; i++)
                    if (lbGameModeMapList.GetItem(1, i).Text.Contains(r))
                    {
                        lbGameModeMapList.SelectedIndex = i;
                        break;
                    }
            }
        }

        private void TbMapSearch_InputReceived(object sender, EventArgs e) => ListMaps();

        private void Dropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (disableGameOptionUpdateBroadcast)
                return;

            var dd = (GameLobbyDropDown)sender;
            dd.HostSelectedIndex = dd.SelectedIndex;
            OnGameOptionChanged();

            if (dd.ControlName != null && dd.ControlIndex != null && dd.ControlIndex.Count == dd.ControlName.Count && dd.SelectedIndex < dd.ControlName.Count)
            {

                List<string> ControlName = dd.ControlName[dd.SelectedIndex].Split('|').ToList();
                List<string> ControlIndex = dd.ControlIndex[dd.SelectedIndex].Split('|').ToList();

                for (int i = 0; i < ControlName.Count; i++)
                {

                    if (!string.IsNullOrEmpty(dd.ControlIndex[dd.SelectedIndex]))
                    {
                        GameLobbyCheckBox otherChk = CheckBoxes.Find(chk => chk.Name == ControlName[i]);
                        if (otherChk != null)
                        {
                            otherChk.Checked = Convert.ToInt32(ControlIndex[i]) != 0;
                        }
                        else
                        {
                            GameLobbyDropDown otherDd = DropDowns.Find(dd => dd.Name == ControlName[i]);
                            otherDd.SelectedIndex = Convert.ToInt32(ControlIndex[i]);
                        }
                    }
                }

            }
        }


        private void ChkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (disableGameOptionUpdateBroadcast)
                return;

            var checkBox = (GameLobbyCheckBox)sender;


            if (checkBox.ControlName != null && checkBox.ControlIndex != null && checkBox.ControlIndex.Count == checkBox.ControlIndex.Count)
            {
                for (int i = 0; i < checkBox.ControlName.Count; i++)
                {

                    if (checkBox.Checked)
                    {
                        GameLobbyCheckBox otherChk = CheckBoxes.Find(chk => chk.Name == checkBox.ControlName[i]);
                        if (otherChk != null)
                            otherChk.Checked = Convert.ToInt32(checkBox.ControlIndex[i]) == 0 ? false : true;
                        else
                        {
                            GameLobbyDropDown otherDd = DropDowns.Find(dd => dd.Name == checkBox.ControlName[i]);
                            otherDd.SelectedIndex = Convert.ToInt32(checkBox.ControlIndex[i]);
                        }
                    }
                }
            }
            checkBox.HostChecked = checkBox.Checked;
            OnGameOptionChanged();

        }

        protected virtual void OnGameOptionChanged()
        {
            CheckDisallowedSides();

            btnLaunchGame.SetRank(GetRank());
        }

        protected void DdGameModeMapFilter_SelectedIndexChanged(object sender, EventArgs e)
        {

            //gameModeMapFilter = ddGameModeMapFilter.SelectedItem.Tag as GameModeMapFilter;
            if (ddGameModeMapFilter.SelectedItem == null) return;

            gameModeMapFilter = new GameModeMapFilter(GetPeopleGameModeMaps(ddGameModeMapFilter.SelectedItem.Text, ddPeople.SelectedIndex));
                

                tbMapSearch.Text = string.Empty;
            tbMapSearch.OnSelectedChanged();

            ListMaps();

            if (lbGameModeMapList.SelectedIndex == -1)
                lbGameModeMapList.SelectedIndex = 0; // Select default GameModeMap
            else
                ChangeMap(GameModeMap);

            if (GameModeMap != null)
            {
                lblModeText.Text = GameModeMap.GameMode.modeText.L10N("UI:ModeText:" + GameModeMap.GameMode.Name);
            }
            else
            {
                lblModeText.Text = string.Empty;
            }

        }

        protected void BtnPlayerExtraOptions_LeftClick(object sender, EventArgs e)
        {
            if (PlayerExtraOptionsPanel.Enabled)
                PlayerExtraOptionsPanel.Disable();
            else
                PlayerExtraOptionsPanel.Enable();
        }

        protected void ApplyPlayerExtraOptions(string sender, string message)
        {
            var playerExtraOptions = PlayerExtraOptions.FromMessage(message);

            if (playerExtraOptions.IsForceRandomSides != PlayerExtraOptionsPanel.IsForcedRandomSides())
                AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomSides, "side selection".L10N("UI:Main:SideAsANoun"));

            if (playerExtraOptions.IsForceRandomColors != PlayerExtraOptionsPanel.IsForcedRandomColors())
                AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomColors, "color selection".L10N("UI:Main:ColorAsANoun"));

            if (playerExtraOptions.IsForceRandomStarts != PlayerExtraOptionsPanel.IsForcedRandomStarts())
                AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomStarts, "start selection".L10N("UI:Main:StartPositionAsANoun"));

            if (playerExtraOptions.IsForceRandomTeams != PlayerExtraOptionsPanel.IsForcedRandomTeams())
                AddPlayerExtraOptionForcedNotice(playerExtraOptions.IsForceRandomTeams, "team selection".L10N("UI:Main:TeamAsANoun"));

            if (playerExtraOptions.IsUseTeamStartMappings != PlayerExtraOptionsPanel.IsUseTeamStartMappings())
                AddPlayerExtraOptionForcedNotice(!playerExtraOptions.IsUseTeamStartMappings, "auto ally".L10N("UI:Main:AutoAllyAsANoun"));

            SetPlayerExtraOptions(playerExtraOptions);
            UpdateMapPreviewBoxEnabledStatus();
        }

        private void AddPlayerExtraOptionForcedNotice(bool disabled, string type)
            => AddNotice(disabled ?
                string.Format("The game host has disabled {0}".L10N("UI:Main:HostDisableSection"), type) :
                string.Format("The game host has enabled {0}".L10N("UI:Main:HostEnableSection"), type));

        private List<GameModeMap> GetSortedGameModeMaps()
        {
            var gameModeMaps = gameModeMapFilter.GetGameModeMaps();

            // Only apply sort if the map list sort button is available.
            if (btnMapSortAlphabetically.Enabled && btnMapSortAlphabetically.Visible)
            {
                switch ((SortDirection)UserINISettings.Instance.MapSortState.Value)
                {
                    case SortDirection.Asc:
                        gameModeMaps = gameModeMaps.OrderBy(gmm => gmm.Map.Name).ToList();
                        break;
                    case SortDirection.Desc:
                        gameModeMaps = gameModeMaps.OrderByDescending(gmm => gmm.Map.Name).ToList();
                        break;
                }
            }

            return gameModeMaps;
        }


        
        protected void ListMaps()
        {
            lbGameModeMapList.SelectedIndexChanged -= LbGameModeMapList_SelectedIndexChanged;

            lbGameModeMapList.ClearItems();
            lbGameModeMapList.SetTopIndex(0);

            lbGameModeMapList.SelectedIndex = -1;

            int mapIndex = -1;
            int skippedMapsCount = 0;

            var isFavoriteMapsSelected = IsFavoriteMapsSelected();
            var maps = GetSortedGameModeMaps();

            maps = [.. maps.OrderBy(o => o.Map?.MaxPlayers)];

            for (int i = 0; i < maps.Count; i++)
            {


                var gameModeMap = maps[i];

                if (tbMapSearch.Text != tbMapSearch.Suggestion)
                {
                    if (!gameModeMap.Map.Name.ToUpper().Contains(tbMapSearch.Text.ToUpper()))
                    {
                        skippedMapsCount++;
                        continue;
                    }
                }

                XNAListBoxItem rankItem = new XNAListBoxItem();
                if (gameModeMap?.Map?.IsCoop??false)
                {
                    if (StatisticsManager.Instance.HasBeatCoOpMap(gameModeMap.Map.Name, gameModeMap.GameMode.UIName))
                        rankItem.Texture = RankTextures[Math.Abs(2 - gameModeMap.GameMode.CoopDifficultyLevel) + 1];
                    else
                        rankItem.Texture = RankTextures[0];
                }
                else
                {

                    rankItem.Texture = RankTextures[GetDefaultMapRankIndex(gameModeMap) + 1];
                }

                XNAListBoxItem mapNameItem = new XNAListBoxItem();
                var mapNameText = gameModeMap.Map?.Name ?? "";
                if (isFavoriteMapsSelected)
                    mapNameText += $" - {gameModeMap.GameMode.UIName}";

                mapNameItem.Text = Renderer.GetSafeString(mapNameText, lbGameModeMapList.FontIndex);

                if ((gameModeMap.Map?.MultiplayerOnly ?? false || gameModeMap.GameMode.MultiplayerOnly) && !isMultiplayer)
                    mapNameItem.TextColor = UISettings.ActiveSettings.DisabledItemColor;
                mapNameItem.Tag = gameModeMap;

                XNAListBoxItem[] mapInfoArray = {
                    rankItem,
                    mapNameItem,
                };

                lbGameModeMapList.AddItem(mapInfoArray);

                if (gameModeMap == GameModeMap)
                    mapIndex = i - skippedMapsCount;
            }


            //    foreach (XNAListBoxItem[] mapInfoArray in maplist)
            //    {
            //       lbGameModeMapList.AddItem(mapInfoArray);
            //  }

            if (mapIndex > -1)
            {
                lbGameModeMapList.SelectedIndex = mapIndex;
                while (mapIndex > lbGameModeMapList.LastIndex)
                    lbGameModeMapList.TopIndex++;
            }

            lbGameModeMapList.SelectedIndexChanged += LbGameModeMapList_SelectedIndexChanged;
        }

        protected abstract int GetDefaultMapRankIndex(GameModeMap gameModeMap);

        private void LbGameModeMapList_RightClick(object sender, EventArgs e)
        {
            if (lbGameModeMapList.HoveredIndex < 0 || lbGameModeMapList.HoveredIndex >= lbGameModeMapList.ItemCount)
                return;

            lbGameModeMapList.SelectedIndex = lbGameModeMapList.HoveredIndex;

            if (!mapContextMenu.Items.Any(i => i.VisibilityChecker == null || i.VisibilityChecker()))
                return;

            toggleFavoriteItem.Text = GameModeMap.IsFavorite ? "Remove Favorite".L10N("UI:Main:RemoveFavorite") : "Add Favorite".L10N("UI:Main:AddFavorite");

            mapContextMenu.Open(GetCursorPoint());
        }

        private bool CanDeleteMap()
        {
            return Map != null && !Map.Official && !isMultiplayer;
        }

        private void DeleteMapConfirmation()
        {
            if (Map == null)
                return;

            var messageBox = XNAMessageBox.ShowYesNoDialog(WindowManager, "Delete Confirmation".L10N("UI:Main:DeleteMapConfirmTitle"),
                string.Format("Are you sure you wish to delete the custom map {0}?".L10N("UI:Main:DeleteMapConfirmText"), Map.Name));
            messageBox.YesClickedAction = DeleteSelectedMap;
        }

        private void MapPreviewBox_ToggleFavorite(object sender, EventArgs e) =>
            ToggleFavoriteMap();

        protected virtual void ToggleFavoriteMap()
        {
            if (GameModeMap != null)
            {
                GameModeMap.IsFavorite = UserINISettings.Instance.ToggleFavoriteMap(Map.Name, GameMode.Name, GameModeMap.IsFavorite);
                MapPreviewBox.RefreshFavoriteBtn();
            }
        }

        protected void RefreshForFavoriteMapRemoved()
        {
            if (!gameModeMapFilter.GetGameModeMaps().Any())
            {
                LoadDefaultGameModeMap();
                return;
            }

            //ListMaps();
            if (IsFavoriteMapsSelected())
                lbGameModeMapList.SelectedIndex = 0; // the map was removed while viewing favorites
        }

        private void DeleteSelectedMap(XNAMessageBox messageBox)
        {
            try
            {
                MapLoader.DeleteCustomMap(GameModeMap);

                tbMapSearch.Text = string.Empty;
                if (GameMode.Maps.Count == 0)
                {
                    // this will trigger another GameMode to be selected
                    GameModeMap = GameModeMaps.Find(gm => gm.GameMode.Maps.Count > 0);
                }
                else
                {
                    // this will trigger another Map to be selected
                    lbGameModeMapList.SelectedIndex = lbGameModeMapList.SelectedIndex == 0 ? 1 : lbGameModeMapList.SelectedIndex - 1;
                }

                ListMaps();
                ChangeMap(GameModeMap);
            }
            catch (IOException ex)
            {
                Logger.Log($"Deleting map {Map.BaseFilePath} failed! message: {ex.Message}");
                XNAMessageBox.Show(WindowManager, "Deleting Map Failed".L10N("UI:Main:DeleteMapFailedTitle"),
                    "Deleting map failed! Reason:".L10N("UI:Main:DeleteMapFailedText") + " " + ex.Message);
            }
        }

        private void LbGameModeMapList_SelectedIndexChanged(object sender, EventArgs e)
        {


            if (lbGameModeMapList.SelectedIndex < 0 || lbGameModeMapList.SelectedIndex >= lbGameModeMapList.ItemCount)
            {
                ChangeMap(null);
                return;
            }

            ReloadMod();

            XNAListBoxItem item = lbGameModeMapList.GetItem(1, lbGameModeMapList.SelectedIndex);


            GameModeMap = (GameModeMap)item.Tag;

            // if(GameModeMap != null)
            ChangeMap(GameModeMap);

            cmbGame.OnSelectedChanged();
        }

        public void ReloadMod()
        {
            if (Map != null)
            {

                //if (!Map.ExtensionOn)
                //{
                //    chkExtension.AllowChecking = false;
                //    chkExtension.Checked = false;
                //}
                //else
                //{
                //    chkExtension.AllowChecking = true;
                //}
                cmbGame.Items.Clear();
                if (Map.Mod != null && Map.Mod.Count != 0)
                {
                    foreach (var m in Map.Mod)
                    {
                        Mod mod = Mod.Mods.Find(i => i.ID == m && i.MuVisible);
                        if (mod != null)
                            cmbGame.AddItem(new XNADropDownItem() { Text = mod.Name, Tag = mod });
                    }
                }
                else
                {
                    foreach (var mod in Mod.Mods)
                    {
                        if (mod.MuVisible)
                        {
                            cmbGame.AddItem(new XNADropDownItem() { Text = mod.Name, Tag = mod });
                        }
                    }
                }
            }
        }

        private void PickRandomMap()
        {
            int totalPlayerCount = Players.Count(p => p.SideId < ddPlayerSides[0].Items.Count - 1)
                   + AIPlayers.Count;
            List<Map> maps = GetMapList(totalPlayerCount);
            if (maps.Count < 1)
                return;

            int random = new Random().Next(0, maps.Count);
            GameModeMap = GameModeMaps.Find(gmm => gmm.GameMode == GameMode && gmm.Map == maps[random]);

            Logger.Log("PickRandomMap: Rolled " + random + " out of " + maps.Count + ". Picked map: " + Map.Name);

            ChangeMap(GameModeMap);
            tbMapSearch.Text = string.Empty;
            tbMapSearch.OnSelectedChanged();
            ListMaps();
        }

        private List<Map> GetMapList(int playerCount)
        {
            List<Map> mapList = (GameMode?.Maps.Where(x => x.MaxPlayers == playerCount) ?? Array.Empty<Map>()).ToList();
            if (mapList.Count < 1 && playerCount <= MAX_PLAYER_COUNT)
                return GetMapList(playerCount + 1);
            else
                return mapList;
        }

        /// <summary>
        /// Refreshes the map selection UI to match the currently selected map
        /// and game mode.
        /// </summary>
        protected void RefreshMapSelectionUI()
        {
            if (GameMode == null)
                return;

            int gameModeMapFilterIndex = ddGameModeMapFilter.Items.FindIndex(i => i.Text == GameMode.UIName);

            if (gameModeMapFilterIndex == -1)
                return;

            if (ddGameModeMapFilter.SelectedIndex == gameModeMapFilterIndex)
                DdGameModeMapFilter_SelectedIndexChanged(this, EventArgs.Empty);

            ddGameModeMapFilter.SelectedIndex = gameModeMapFilterIndex;
        }

        /// <summary>
        /// Initializes the player option drop-down controls.
        /// </summary>
        protected void InitPlayerOptionDropdowns()
        {
            ddPlayerNames = new XNAClientDropDown[MAX_PLAYER_COUNT];
            ddPlayerSides = new XNAClientDropDown[MAX_PLAYER_COUNT];
            ddPlayerColors = new XNAClientDropDown[MAX_PLAYER_COUNT];
            ddPlayerStarts = new XNAClientDropDown[MAX_PLAYER_COUNT];
            ddPlayerTeams = new XNAClientDropDown[MAX_PLAYER_COUNT];

            int playerOptionVecticalMargin = ConfigIni.GetIntValue(Name, "PlayerOptionVerticalMargin", PLAYER_OPTION_VERTICAL_MARGIN);
            int playerOptionHorizontalMargin = ConfigIni.GetIntValue(Name, "PlayerOptionHorizontalMargin", PLAYER_OPTION_HORIZONTAL_MARGIN);
            int playerOptionCaptionLocationY = ConfigIni.GetIntValue(Name, "PlayerOptionCaptionLocationY", PLAYER_OPTION_CAPTION_Y);
            int playerNameWidth = ConfigIni.GetIntValue(Name, "PlayerNameWidth", 136);
            int sideWidth = ConfigIni.GetIntValue(Name, "SideWidth", 91);
            int colorWidth = ConfigIni.GetIntValue(Name, "ColorWidth", 79);
            int startWidth = ConfigIni.GetIntValue(Name, "StartWidth", 49);
            int teamWidth = ConfigIni.GetIntValue(Name, "TeamWidth", 46);
            int locationX = ConfigIni.GetIntValue(Name, "PlayerOptionLocationX", 25);
            int locationY = ConfigIni.GetIntValue(Name, "PlayerOptionLocationY", 24);

            // InitPlayerOptionDropdowns(136, 91, 79, 49, 46, new Point(25, 24));

            string[] sides = ClientConfiguration.Instance.Sides.Split(',');
            SideCount = sides.Length;

            List<string> selectorNames = new List<string>();
            GetRandomSelectors(selectorNames, RandomSelectors);
            RandomSelectorCount = RandomSelectors.Count + 1;
            MapPreviewBox.RandomSelectorCount = RandomSelectorCount;

            string randomColor = GameOptionsIni.GetStringValue("General", "RandomColor", "255,255,255");

            for (int i = MAX_PLAYER_COUNT - 1; i > -1; i--)
            {
                var ddPlayerName = new XNAClientDropDown(WindowManager);
                ddPlayerName.Name = "ddPlayerName" + i;
                ddPlayerName.ClientRectangle = new Rectangle(locationX,
                    locationY + (DROP_DOWN_HEIGHT + playerOptionVecticalMargin) * i,
                    playerNameWidth, DROP_DOWN_HEIGHT);
                ddPlayerName.AddItem(String.Empty);
                ProgramConstants.AI_PLAYER_NAMES.ForEach(ddPlayerName.AddItem);
                ddPlayerName.AllowDropDown = true;
                ddPlayerName.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerName.RightClick += MultiplayerName_RightClick;
                ddPlayerName.Tag = true;

                var ddPlayerSide = new XNAClientDropDown(WindowManager);
                ddPlayerSide.Name = "ddPlayerSide" + i;
                ddPlayerSide.ClientRectangle = new Rectangle(
                    ddPlayerName.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y, sideWidth, DROP_DOWN_HEIGHT);
                ddPlayerSide.AddItem("Random".L10N("UI:Main:RandomSide"), LoadTextureOrNull("randomicon.png"));
                foreach (string randomSelector in selectorNames)
                    ddPlayerSide.AddItem(randomSelector, LoadTextureOrNull(randomSelector + "icon.png"));
                foreach (string sideName in sides)
                    ddPlayerSide.AddItem(sideName, LoadTextureOrNull(sideName + "icon.png"));
                ddPlayerSide.AllowDropDown = false;
                ddPlayerSide.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerSide.Tag = true;

                var ddPlayerColor = new XNAClientDropDown(WindowManager);
                ddPlayerColor.Name = "ddPlayerColor" + i;
                ddPlayerColor.ClientRectangle = new Rectangle(
                    ddPlayerSide.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y, colorWidth, DROP_DOWN_HEIGHT);
                ddPlayerColor.AddItem("Random".L10N("UI:Main:Random"), AssetLoader.GetColorFromString(randomColor));
                foreach (MultiplayerColor mpColor in MPColors)
                    ddPlayerColor.AddItem(mpColor.Name, mpColor.TextColor);
                ddPlayerColor.AllowDropDown = false;
                ddPlayerColor.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerColor.Tag = false;

                var ddPlayerTeam = new XNAClientDropDown(WindowManager);
                ddPlayerTeam.Name = "ddPlayerTeam" + i;
                ddPlayerTeam.ClientRectangle = new Rectangle(
                    ddPlayerColor.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y, teamWidth, DROP_DOWN_HEIGHT);
                ddPlayerTeam.AddItem("-");
                ProgramConstants.TEAMS.ForEach(ddPlayerTeam.AddItem);
                ddPlayerTeam.AllowDropDown = false;
                ddPlayerTeam.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerTeam.Tag = true;

                var ddPlayerStart = new XNAClientDropDown(WindowManager);
                ddPlayerStart.Name = "ddPlayerStart" + i;
                ddPlayerStart.ClientRectangle = new Rectangle(
                    ddPlayerTeam.Right + playerOptionHorizontalMargin,
                    ddPlayerName.Y, startWidth, DROP_DOWN_HEIGHT);
                for (int j = 1; j < 9; j++)
                    ddPlayerStart.AddItem(j.ToString());
                ddPlayerStart.AllowDropDown = false;
                ddPlayerStart.SelectedIndexChanged += CopyPlayerDataFromUI;
                ddPlayerStart.Visible = false;
                ddPlayerStart.Enabled = false;
                ddPlayerStart.Tag = true;

                ddPlayerNames[i] = ddPlayerName;
                ddPlayerSides[i] = ddPlayerSide;
                ddPlayerColors[i] = ddPlayerColor;
                ddPlayerStarts[i] = ddPlayerStart;
                ddPlayerTeams[i] = ddPlayerTeam;

                PlayerOptionsPanel.AddChild(ddPlayerName);
                PlayerOptionsPanel.AddChild(ddPlayerSide);
                PlayerOptionsPanel.AddChild(ddPlayerColor);
                PlayerOptionsPanel.AddChild(ddPlayerStart);
                PlayerOptionsPanel.AddChild(ddPlayerTeam);

                ReadINIForControl(ddPlayerName);
                ReadINIForControl(ddPlayerSide);
                ReadINIForControl(ddPlayerColor);
                ReadINIForControl(ddPlayerStart);
                ReadINIForControl(ddPlayerTeam);
            }

            var lblName = GeneratePlayerOptionCaption("lblName", "PLAYER".L10N("UI:Main:PlayerOptionPlayer"), ddPlayerNames[0].X, playerOptionCaptionLocationY);
            var lblSide = GeneratePlayerOptionCaption("lblSide", "SIDE".L10N("UI:Main:PlayerOptionSide"), ddPlayerSides[0].X, playerOptionCaptionLocationY);
            var lblColor = GeneratePlayerOptionCaption("lblColor", "COLOR".L10N("UI:Main:PlayerOptionColor"), ddPlayerColors[0].X, playerOptionCaptionLocationY);

            var lblStart = GeneratePlayerOptionCaption("lblStart", "START".L10N("UI:Main:PlayerOptionStart"), ddPlayerStarts[0].X, playerOptionCaptionLocationY);
            lblStart.Visible = false;

            var lblTeam = GeneratePlayerOptionCaption("lblTeam", "TEAM".L10N("UI:Main:PlayerOptionTeam"), ddPlayerTeams[0].X, playerOptionCaptionLocationY);

            ReadINIForControl(lblName);
            ReadINIForControl(lblSide);
            ReadINIForControl(lblColor);
            ReadINIForControl(lblStart);
            ReadINIForControl(lblTeam);

            btnPlayerExtraOptionsOpen = FindChild<XNAClientButton>(nameof(btnPlayerExtraOptionsOpen), true);
            if (btnPlayerExtraOptionsOpen != null)
            {
                PlayerExtraOptionsPanel = FindChild<PlayerExtraOptionsPanel>(nameof(PlayerExtraOptionsPanel));
                PlayerExtraOptionsPanel.Disable();
                PlayerExtraOptionsPanel.OptionsChanged += PlayerExtraOptions_OptionsChanged;
                btnPlayerExtraOptionsOpen.LeftClick += BtnPlayerExtraOptions_LeftClick;
            }

            CheckDisallowedSides();
        }

        private XNALabel GeneratePlayerOptionCaption(string name, string text, int x, int y)
        {
            var label = new XNALabel(WindowManager);
            label.Name = name;
            label.Text = text;
            label.FontIndex = 1;
            label.ClientRectangle = new Rectangle(x, y, 0, 0);
            PlayerOptionsPanel.AddChild(label);

            return label;
        }

        protected virtual void PlayerExtraOptions_OptionsChanged(object sender, EventArgs e)
        {
            var playerExtraOptions = GetPlayerExtraOptions();

            for (int i = 0; i < ddPlayerSides.Length; i++)
                EnablePlayerOptionDropDown(ddPlayerSides[i], i, !playerExtraOptions.IsForceRandomSides);

            for (int i = 0; i < ddPlayerTeams.Length; i++)
                EnablePlayerOptionDropDown(ddPlayerTeams[i], i, !playerExtraOptions.IsForceRandomTeams);

            for (int i = 0; i < ddPlayerColors.Length; i++)
                EnablePlayerOptionDropDown(ddPlayerColors[i], i, !playerExtraOptions.IsForceRandomColors);

            for (int i = 0; i < ddPlayerStarts.Length; i++)
                EnablePlayerOptionDropDown(ddPlayerStarts[i], i, !playerExtraOptions.IsForceRandomStarts);

            UpdateMapPreviewBoxEnabledStatus();
            RefreshBtnPlayerExtraOptionsOpenTexture();
        }

        private void EnablePlayerOptionDropDown(XNAClientDropDown clientDropDown, int playerIndex, bool enable)
        {
            var pInfo = GetPlayerInfoForIndex(playerIndex);
            var allowOtherPlayerOptionsChange = AllowPlayerOptionsChange() && pInfo != null;
            clientDropDown.AllowDropDown = enable && (allowOtherPlayerOptionsChange || pInfo?.Name == ProgramConstants.PLAYERNAME);
            if (!clientDropDown.AllowDropDown)
                clientDropDown.SelectedIndex = clientDropDown.SelectedIndex > 0 ? 0 : clientDropDown.SelectedIndex;
        }

        protected PlayerInfo GetPlayerInfoForIndex(int playerIndex)
        {
            if (playerIndex < Players.Count)
                return Players[playerIndex];

            if (playerIndex < Players.Count + AIPlayers.Count)
                return AIPlayers[playerIndex - Players.Count];

            return null;
        }

        protected PlayerExtraOptions GetPlayerExtraOptions() =>
            PlayerExtraOptionsPanel == null ? new PlayerExtraOptions() : PlayerExtraOptionsPanel.GetPlayerExtraOptions();

        protected void SetPlayerExtraOptions(PlayerExtraOptions playerExtraOptions) => PlayerExtraOptionsPanel?.SetPlayerExtraOptions(playerExtraOptions);

        protected string GetTeamMappingsError() => GetPlayerExtraOptions()?.GetTeamMappingsError();

        private Texture2D LoadTextureOrNull(string name) =>
            AssetLoader.AssetExists(name) ? AssetLoader.LoadTexture(name) : null;

        /// <summary>
        /// Loads random side selectors from GameOptions.ini
        /// </summary>
        /// <param name="selectorNames">TODO comment</param>
        /// <param name="selectorSides">TODO comment</param>
        private void GetRandomSelectors(List<string> selectorNames, List<int[]> selectorSides)
        {
            List<string> keys = GameOptionsIni.GetSectionKeys("RandomSelectors");

            if (keys == null)
                return;

            foreach (string randomSelector in keys)
            {
                List<int> randomSides = new List<int>();
                try
                {
                    string[] tmp = GameOptionsIni.GetStringValue("RandomSelectors", randomSelector, string.Empty).Split(',');
                    randomSides = Array.ConvertAll(tmp, int.Parse).Distinct().ToList();
                    randomSides.RemoveAll(x => (x >= SideCount || x < 0));
                }
                catch (FormatException) { }

                if (randomSides.Count > 1)
                {
                    selectorNames.Add(randomSelector);
                    selectorSides.Add(randomSides.ToArray());
                }
            }
        }

        protected abstract void BtnLaunchGame_LeftClick(object sender, EventArgs e);

        protected abstract void BtnLeaveGame_LeftClick(object sender, EventArgs e);

        /// <summary>
        /// Updates Discord Rich Presence with actual information.
        /// </summary>
        /// <param name="resetTimer">Whether to restart the "Elapsed" timer or not</param>
        protected abstract void UpdateDiscordPresence(bool resetTimer = false);

        /// <summary>
        /// Resets Discord Rich Presence to default state.
        /// </summary>
        protected void ResetDiscordPresence() => discordHandler.UpdatePresence();

        protected void LoadDefaultGameModeMap()
        {
            if (ddGameModeMapFilter.Items.Count > 0)
            {
                ddGameModeMapFilter.SelectedIndex = GetDefaultGameModeMapFilterIndex();

                lbGameModeMapList.SelectedIndex = 0;
            }
        }

        protected int GetDefaultGameModeMapFilterIndex()
        {
            return ddGameModeMapFilter.Items.FindIndex(i => (i.Tag as GameModeMapFilter)?.Any() ?? false);
        }

        protected GameModeMapFilter GetDefaultGameModeMapFilter()
        {
            var i = GetDefaultGameModeMapFilterIndex();
            if (i != -1 && i < ddGameModeMapFilter.Items.Count)
                return ddGameModeMapFilter.Items[i].Tag as GameModeMapFilter;
            else
                return null;
        }

        private int GetSpectatorSideIndex() => SideCount + RandomSelectorCount;

        /// <summary>
        /// Applies disallowed side indexes to the side option drop-downs
        /// and player options.
        /// </summary>
        protected void CheckDisallowedSides()
        {
            var disallowedSideArray = GetDisallowedSides();
            int defaultSide = 0;
            int allowedSideCount = disallowedSideArray.Count(b => !b);

            if (allowedSideCount == 1)
            {
                // Disallow Random

                for (int i = 0; i < disallowedSideArray.Length; i++)
                {
                    if (!disallowedSideArray[i])
                        defaultSide = i + RandomSelectorCount;
                }

                foreach (XNADropDown dd in ddPlayerSides)
                {
                    //dd.Items[0].Selectable = false;
                    for (int i = 0; i < RandomSelectorCount; i++)
                        dd.Items[i].Selectable = false;
                }
            }
            else
            {
                foreach (XNADropDown dd in ddPlayerSides)
                {
                    //dd.Items[0].Selectable = true;
                    for (int i = 0; i < RandomSelectorCount; i++)
                        dd.Items[i].Selectable = true;
                }
            }

            var concatPlayerList = Players.Concat(AIPlayers);

            // Disable custom random groups if all or all except one of included sides are unavailable.
            int c = 0;
            var playerInfos = concatPlayerList.ToList();
            foreach (int[] randomSides in RandomSelectors)
            {
                int disableCount = 0;

                foreach (int side in randomSides)
                {
                    if (disallowedSideArray[side])
                        disableCount++;
                }

                bool disabled = disableCount >= randomSides.Length - 1;

                foreach (XNADropDown dd in ddPlayerSides)
                    dd.Items[1 + c].Selectable = !disabled;

                foreach (PlayerInfo pInfo in playerInfos)
                {
                    if (pInfo.SideId == 1 + c && disabled)
                        pInfo.SideId = defaultSide;
                }

                c++;
            }

            // Go over the side array and either disable or enable the side
            // dropdown options depending on whether the side is available
            for (int i = 0; i < disallowedSideArray.Length; i++)
            {
                bool disabled = disallowedSideArray[i];

                if (disabled)
                {
                    foreach (XNADropDown dd in ddPlayerSides)
                        dd.Items[i + RandomSelectorCount].Selectable = false;

                    // Change the sides of players that use the disabled
                    // side to the default side
                    foreach (PlayerInfo pInfo in playerInfos)
                    {
                        if (pInfo.SideId == i + RandomSelectorCount)
                            pInfo.SideId = defaultSide;
                    }
                }
                else
                {
                    foreach (XNADropDown dd in ddPlayerSides)
                        dd.Items[i + RandomSelectorCount].Selectable = true;
                }
            }

            // If only 1 side is allowed, change all players' sides to that
            if (allowedSideCount == 1)
            {
                foreach (PlayerInfo pInfo in playerInfos)
                {
                    if (pInfo.SideId == 0)
                        pInfo.SideId = defaultSide;
                }
            }

            if (Map != null && Map.CoopInfo != null)
            {
                // Disallow spectator

                foreach (PlayerInfo pInfo in playerInfos)
                {
                    if (pInfo.SideId == GetSpectatorSideIndex())
                        pInfo.SideId = defaultSide;
                }

                foreach (XNADropDown dd in ddPlayerSides)
                {
                    if (dd.Items.Count > GetSpectatorSideIndex())
                        dd.Items[SideCount + RandomSelectorCount].Selectable = false;
                }
            }
            else
            {
                foreach (XNADropDown dd in ddPlayerSides)
                {
                    if (dd.Items.Count > SideCount + RandomSelectorCount)
                        dd.Items[SideCount + RandomSelectorCount].Selectable = true;
                }
            }
        }

        public List<string> GetDeleteFile(string oldGame)
        {
            if (string.IsNullOrEmpty(oldGame) || !Directory.Exists(oldGame))
                return null;
            List<string> deleteFile = new List<string>();

            foreach (string file in Directory.GetFiles(oldGame))
            {
                deleteFile.Add(Path.GetFileName(file));
            }

            return deleteFile;
        }

        /// <summary>
        /// Gets a list of side indexes that are disallowed.
        /// </summary>
        /// <returns>A list of disallowed side indexes.</returns>
        protected bool[] GetDisallowedSides()
        {
            string[] sides = null;
            List<string> selectorNames = new List<string>();
            RandomSelectors.Clear();
            string[,] Randomside = null;
            int count = 0;
            foreach (var dropDown in DropDowns)
            {

                if (dropDown.SetSides() != null)
                {
                    sides = dropDown.SetSides();

                }

            }

            if (sides != null)
            {

                foreach (var dropDown in DropDowns)
                {
                    if (dropDown.Name == "cmbGame")
                        Randomside = dropDown.SetRandomSelectors();

                }
                if (Randomside != null)
                {

                    count = Randomside.GetLength(0);

                    MapPreviewBox.RandomSelectorCount = count;

                }
                RandomSelectorCount = count + 1;
                SideCount = sides.Length;
            }
            else
            {

                sides = GameOptionsIni.GetStringValue("General", "Sides", String.Empty).Split(',');
                GetRandomSelectors(selectorNames, RandomSelectors);
                RandomSelectorCount = RandomSelectors.Count + 1;
                count = RandomSelectors.Count;
                Randomside = new string[selectorNames.Count, 2];
                for (int i = 0; i < selectorNames.Count; i++)
                {
                    Randomside[i, 0] = selectorNames[i];

                    Randomside[i, 1] = string.Join(",", RandomSelectors[i]);

                }
                SideCount = sides.Length;
            }



            foreach (var ddSide in ddPlayerSides)
            {
                ddSide.Items.Clear();
                ddSide.AddItem("Random".L10N("UI:Main:Random"), LoadTextureOrNull("Randomicon.png"));
                RandomSelectors.Clear();
                for (int i = 0; i < count; i++)
                {
                    RandomSelectors.Add(Array.ConvertAll(Randomside[i, 1].Split(','), int.Parse));
                    ddSide.AddItem(Randomside[i, 0].L10N($"UI:Side:{Randomside[i, 0]}"), LoadTextureOrNull(Randomside[i, 0] + "icon.png"));
                }

                for (int i = count; i < sides.Length + count; i++)
                {
                    ddSide.AddItem(sides[i - count].L10N($"UI:Side:{sides[i - count]}"), LoadTextureOrNull(sides[i - count] + "icon.png"));

                }
                ddSide.AddItem("spectator".L10N("UI:Main:spectator"), LoadTextureOrNull("spectatoricon.png"));
                //if (ddSide.SelectedItem == null)
                //    ddSide.SelectedIndex = 0;
            }

            //foreach(var ddcolor in ddPlayerColors)
            //{
            //    if (ddcolor.SelectedItem == null)
            //        ddcolor.SelectedIndex = 0;
            //}

            //foreach(var ddSide in ddPlayerSides)
            //{
            //    if (ddSide.SelectedItem == null)
            //        ddSide.SelectedIndex = 0;
            //}

            var returnValue = new bool[SideCount];

            foreach (var dropDown in DropDowns)
            {
                dropDown.ApplyDisallowedSideIndex(returnValue);
            }

            if (Map != null && Map.CoopInfo != null)
            {
                // Co-Op map disallowed side logic

                foreach (int disallowedSideIndex in Map.CoopInfo.DisallowedPlayerSides)
                    returnValue[disallowedSideIndex] = true;
            }

            if (GameMode != null)
            {
                foreach (int disallowedSideIndex in GameMode.DisallowedPlayerSides)
                    returnValue[disallowedSideIndex] = true;
            }

            foreach (var checkBox in CheckBoxes)
                checkBox.ApplyDisallowedSideIndex(returnValue);



            return returnValue;
        }

        /// <summary>
        /// Randomizes options of both human and AI players
        /// and returns the options as an array of PlayerHouseInfos.
        /// </summary>
        /// <returns>An array of PlayerHouseInfos.</returns>
        protected virtual PlayerHouseInfo[] Randomize(List<TeamStartMapping> teamStartMappings)
        {
            int totalPlayerCount = Players.Count + AIPlayers.Count;
            PlayerHouseInfo[] houseInfos = new PlayerHouseInfo[totalPlayerCount];

            for (int i = 0; i < totalPlayerCount; i++)
                houseInfos[i] = new PlayerHouseInfo();

            // Gather list of spectators
            for (int i = 0; i < Players.Count; i++)
                houseInfos[i].IsSpectator = Players[i].SideId == GetSpectatorSideIndex();

            // Gather list of available colors

            List<int> freeColors = new List<int>();

            for (int cId = 0; cId < MPColors.Count; cId++)
                freeColors.Add(cId);

            if (Map.CoopInfo != null)
            {
                foreach (int colorIndex in Map.CoopInfo.DisallowedPlayerColors)
                    freeColors.Remove(colorIndex);
            }

            foreach (PlayerInfo player in Players)
                freeColors.Remove(player.ColorId - 1); // The first color is Random

            foreach (PlayerInfo aiPlayer in AIPlayers)
                freeColors.Remove(aiPlayer.ColorId - 1);

            // Gather list of available starting locations

            List<int> freeStartingLocations = new List<int>();
            List<int> takenStartingLocations = new List<int>();

            for (int i = 0; i < Map.MaxPlayers; i++)
                freeStartingLocations.Add(i);

            for (int i = 0; i < Players.Count; i++)
            {
                if (!houseInfos[i].IsSpectator)
                {
                    freeStartingLocations.Remove(Players[i].StartingLocation - 1);
                    //takenStartingLocations.Add(Players[i].StartingLocation - 1);
                    // ^ Gives everyone with a selected location a completely random
                    // location in-game, because PlayerHouseInfo.RandomizeStart already
                    // fills the list itself
                }
            }

            for (int i = 0; i < AIPlayers.Count; i++)
                freeStartingLocations.Remove(AIPlayers[i].StartingLocation - 1);

            foreach (var teamStartMapping in teamStartMappings.Where(mapping => mapping.IsBlock))
                freeStartingLocations.Remove(teamStartMapping.StartingWaypoint);

            // Randomize options

            Random random = new Random(RandomSeed);

            for (int i = 0; i < totalPlayerCount; i++)
            {
                PlayerInfo pInfo;
                PlayerHouseInfo pHouseInfo = houseInfos[i];

                if (i < Players.Count)
                    pInfo = Players[i];
                else
                    pInfo = AIPlayers[i - Players.Count];

                pHouseInfo.RandomizeSide(pInfo, SideCount, random, GetDisallowedSides(), RandomSelectors, RandomSelectorCount);

                // 在这里

              //  pHouseInfo.RandomizeColor(pInfo, freeColors[..(chkExtension.Checked ? 15 : 8)], MPColors[..(chkExtension.Checked ? 16 : 8)], random);
                pHouseInfo.RandomizeColor(pInfo, freeColors, MPColors, random);

                pHouseInfo.RandomizeStart(pInfo, random, freeStartingLocations, takenStartingLocations, teamStartMappings.Count != 0);
            }

            return houseInfos;
        }


        void UpdateListItems(List<string> s1, List<string> s2, string prefix)
        {
            // 找到s1中匹配前缀的索引
            int index = s1.IndexOf(prefix);

            // 如果找到了匹配项
            if (index != -1)
            {
                // 从s2中找到以prefix开头的项
                var matchingItems = s2.Where(s => s.StartsWith(prefix)).ToList();

                // 如果s2中有以prefix开头的项
                if (matchingItems.Any())
                {
                    // 用s2中的第一个匹配项替换s1中的对应项
                    s1[index] = matchingItems.First();

                    // 将s2中的其它匹配项添加到s1中
                    foreach (var matchingItem in matchingItems.Skip(1))
                    {
                        s1.Add(matchingItem);
                    }
                }
            }
        }

        private Dictionary<string, Dictionary<string, int>> FileHash = [];
        private Dictionary<string, string> FilePaths = [];
        /// <summary>
        /// Writes spawn.ini. Returns the player house info returned from the randomizer.
        /// </summary>
        private PlayerHouseInfo[] WriteSpawnIni()
        {
            Logger.Log("Writing spawn.ini");

            FileInfo spawnerSettingsFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);

         //   var spawnIni = new IniFile(spawnerSettingsFile.FullName);

            string newGame = ((Mod)cmbGame.SelectedItem.Tag).FilePath;
            string newMain = ((Mod)cmbGame.SelectedItem.Tag).md == "md" ?
               "Mod&AI/Main/YR_YR"
               :
               ((AI)cmbAI.SelectedItem.Tag).YR ?
               "Mod&AI/Main/RA2_YR"
               :
               "Mod&AI/Main/RA2_RA2";

            string newExtension = string.Empty;

            if (chkExtension.Checked)
            {
                var extensionGame = ((Mod)cmbGame.SelectedItem.Tag).Extension;
                var extensionAI = ((AI)cmbAI.SelectedItem.Tag).Extension;
                if (extensionAI != null && extensionGame != "NP")
                {
                    var s1 = new List<string>(extensionGame.Split(','));
                    var s2 = new List<string>(extensionAI.Split(','));

                    UpdateListItems(s1, s2, "Ares");
                    UpdateListItems(s1, s2, "Phobos");

                    newExtension = string.Join(",", s1.Intersect(s2));
                }
                else
                {
                    newExtension = extensionGame;
                }

            }

            
            string newMission = Map.Mission;

            
            string newAi = ((AI)cmbAI.SelectedItem.Tag).FilePath;

            
            
            var spawnIni = new IniFile(spawnerSettingsFile.FullName);
            spawnerSettingsFile.Delete();
            if (Map.IsCoop)
            {
                foreach (PlayerInfo pInfo in Players)
                    pInfo.TeamId = 1;

                foreach (PlayerInfo pInfo in AIPlayers)
                    pInfo.TeamId = 1;
            }

            var teamStartMappings = new List<TeamStartMapping>(0);
            if (PlayerExtraOptionsPanel != null)
            {
                teamStartMappings = PlayerExtraOptionsPanel.GetTeamStartMappings();
            }

            PlayerHouseInfo[] houseInfos = Randomize(teamStartMappings);

            bool 加载音乐 = true;

            bool 检测文件是否修改()
            {
                string oldMain = spawnIni.GetValue("Settings", "Main", string.Empty);
                string oldExtension = spawnIni.GetValue("Settings", "Extension", string.Empty);
                string oldGame = spawnIni.GetValue("Settings", "Game", string.Empty);
                string oldMission = spawnIni.GetValue("Settings", "Mission", string.Empty);
                string oldAi = spawnIni.GetValue("Settings", "AI", string.Empty);

                if (oldGame != newGame && File.Exists($"{newGame}\\thememd.mix"))
                    加载音乐 = false;

                 
                if (oldMain != newMain || oldGame != newGame || oldAi != newAi || oldMission != newMission || oldExtension != newExtension) return true;

                foreach (var fileType in FilePaths)
                {
                    if (!FileHash.TryGetValue(fileType.Key, out var value)) return true;
                    foreach (var file in Directory.GetFiles(fileType.Value))
                    {
                        if (!value.TryGetValue(file, out var hash)) return true;
                        var newHash = new FileInfo(file).GetHashCode();
                        if (hash != newHash) return true;
                    }
                }

                return false;
            }

            try
            {
                if (检测文件是否修改())
                {
                    
                    GameOptionsPanel.清除缓存();



                    FileHelper.CopyDirectory(newGame, "./");

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

                    FileHelper.CopyDirectory(newAi, "./");

                    FileHelper.CopyDirectory(newMission, "./");

                    FileHelper.CopyDirectory(newMain, "./");
                }

                if(加载音乐)
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
            catch (FileLockedException ex)
            {
                XNAMessageBox.Show(WindowManager, "错误", ex.Message);
                return null;
            }

            var settings = new IniSection("Settings");

            settings.SetValue("Main", newMain);
            //写入新游戏
            settings.SetValue("Game", newGame);
            //写入新扩展
            settings.SetValue("Extension", newExtension);
            //写入新AI
            settings.SetValue("AI", newAi);

            settings.SetValue("Mission", newMission);

            settings.SetValue("Ra2Mode", newMain == "RA2_RA2");

            settings.SetValue("Name", ProgramConstants.PLAYERNAME);
            settings.SetValue("Scenario", ProgramConstants.SPAWNMAP_INI);
            settings.SetValue("UIGameMode", GameMode.UIName);
            settings.SetValue("UIMapName", Map.Name);
            settings.SetValue("PlayerCount", Players.Count);
            int myIndex = Players.FindIndex(c => c.Name == ProgramConstants.PLAYERNAME);
            settings.SetValue("Side", houseInfos[myIndex].InternalSideIndex);
            settings.SetValue("IsSpectator", houseInfos[myIndex].IsSpectator);
            settings.SetValue("Color", houseInfos[myIndex].ColorIndex);
            settings.SetValue("CustomLoadScreen", LoadingScreenController.GetLoadScreenName(houseInfos[myIndex].InternalSideIndex.ToString()));
            settings.SetValue("AIPlayers", AIPlayers.Count);
            settings.SetValue("Seed", RandomSeed);
            if (GetPvPTeamCount() > 1)
                settings.SetValue("CoachMode", true);
            if (GetGameType() == GameType.Coop)
                settings.SetValue("AutoSurrender", false);
            spawnIni.AddSection(settings);
            WriteSpawnIniAdditions(spawnIni);

            foreach (GameLobbyCheckBox chkBox in CheckBoxes)
                chkBox.ApplySpawnINICode(spawnIni);

            foreach (GameLobbyDropDown dd in DropDowns)
                dd.ApplySpawnIniCode(spawnIni);

            // Apply forced options from GameOptions.ini

            List<string> forcedKeys = GameOptionsIni.GetSectionKeys("ForcedSpawnIniOptions");

            if (forcedKeys != null)
            {
                foreach (string key in forcedKeys)
                {
                    spawnIni.SetValue("Settings", key,
                        GameOptionsIni.GetStringValue("ForcedSpawnIniOptions", key, string.Empty));
                }
            }


            GameMode.ApplySpawnIniCode(spawnIni); // Forced options from the game mode
            Map.ApplySpawnIniCode(spawnIni, Players.Count + AIPlayers.Count,
                AIPlayers.Count, GameMode.CoopDifficultyLevel); // Forced options from the map

            // Player options

            int otherId = 1;

            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];
                PlayerHouseInfo pHouseInfo = houseInfos[pId];

                if (pInfo.Name == ProgramConstants.PLAYERNAME)
                    continue;

                string sectionName = "Other" + otherId;

                spawnIni.SetValue(sectionName, "Name", pInfo.Name);
                spawnIni.SetValue(sectionName, "Side", pHouseInfo.InternalSideIndex);
                spawnIni.SetValue(sectionName, "IsSpectator", pHouseInfo.IsSpectator);
                spawnIni.SetValue(sectionName, "Color", pHouseInfo.ColorIndex);
                spawnIni.SetValue(sectionName, "Ip", GetIPAddressForPlayer(pInfo));
                spawnIni.SetValue(sectionName, "Port", pInfo.Port);

                otherId++;
            }

            // The spawner assigns players to SpawnX houses based on their in-game color index
            List<int> multiCmbIndexes = new List<int>();
            var sortedColorList = MPColors.OrderBy(mpc => mpc.GameColorIndex).ToList();

            for (int cId = 0; cId < sortedColorList.Count; cId++)
            {
                for (int pId = 0; pId < Players.Count; pId++)
                {
                    if (houseInfos[pId].ColorIndex == sortedColorList[cId].GameColorIndex)
                        multiCmbIndexes.Add(pId);
                }
            }

            if (AIPlayers.Count > 0)
            {
                for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
                {
                    int multiId = multiCmbIndexes.Count + aiId + 1;

                    string keyName = "Multi" + multiId;

                    spawnIni.SetValue("HouseHandicaps", keyName, 2 - AIPlayers[aiId].HouseHandicapAILevel);
                    spawnIni.SetValue("HouseCountries", keyName, houseInfos[Players.Count + aiId].InternalSideIndex);
                    spawnIni.SetValue("HouseColors", keyName, houseInfos[Players.Count + aiId].ColorIndex);
                }
            }

            for (int multiId = 0; multiId < multiCmbIndexes.Count; multiId++)
            {
                int pIndex = multiCmbIndexes[multiId];
                if (houseInfos[pIndex].IsSpectator)
                    spawnIni.SetValue("IsSpectator", "Multi" + (multiId + 1), true);
            }

            // Write alliances, the code is pretty big so let's take it to another class
            AllianceHolder.WriteInfoToSpawnIni(Players, AIPlayers, multiCmbIndexes, houseInfos.ToList(), teamStartMappings, spawnIni);

            for (int pId = 0; pId < Players.Count; pId++)
            {
                int startingWaypoint = houseInfos[multiCmbIndexes[pId]].StartingWaypoint;

                // -1 means no starting location at all - let the game itself pick the starting location
                // using its own logic
                if (startingWaypoint > -1)
                {
                    int multiIndex = pId + 1;
                    spawnIni.SetValue("SpawnLocations", "Multi" + multiIndex,
                        startingWaypoint);
                }
            }

            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                int startingWaypoint = houseInfos[Players.Count + aiId].StartingWaypoint;

                if (startingWaypoint > -1)
                {
                    int multiIndex = Players.Count + aiId + 1;
                    spawnIni.SetValue("SpawnLocations", "Multi" + multiIndex,
                        startingWaypoint);
                }
            }

            spawnIni.WriteIniFile();


            oldSaves = Directory.GetFiles($"{ProgramConstants.GamePath}Saved Games");

            return houseInfos;
        }

        /// <summary>
        /// Returns the number of teams with human players in them.
        /// Does not count spectators and human players that don't have a team set.
        /// </summary>
        /// <returns>The number of human player teams in the game.</returns>
        private int GetPvPTeamCount()
        {
            int[] teamPlayerCounts = new int[4];
            int playerTeamCount = 0;

            foreach (PlayerInfo pInfo in Players)
            {
                if (pInfo.IsAI || IsPlayerSpectator(pInfo))
                    continue;

                if (pInfo.TeamId > 0)
                {
                    teamPlayerCounts[pInfo.TeamId - 1]++;
                    if (teamPlayerCounts[pInfo.TeamId - 1] == 2)
                        playerTeamCount++;
                }
            }

            return playerTeamCount;
        }

        /// <summary>
        /// Checks whether the specified player has selected Spectator as their side.
        /// </summary>
        /// <param name="pInfo">The player.</param>
        /// <returns>True if the player is a spectator, otherwise false.</returns>
        protected bool IsPlayerSpectator(PlayerInfo pInfo)
        {
            if (pInfo.SideId == GetSpectatorSideIndex())
                return true;

            return false;
        }

        protected virtual string GetIPAddressForPlayer(PlayerInfo player) => "0.0.0.0";

        /// <summary>
        /// Override this in a derived class to write game lobby specific code to
        /// spawn.ini. For example, CnCNet game lobbies should write tunnel info
        /// in this method.
        /// </summary>
        /// <param name="iniFile">The spawn INI file.</param>
        protected virtual void WriteSpawnIniAdditions(IniFile iniFile)
        {
            // Do nothing by default
        }

        private void InitializeMatchStatistics(PlayerHouseInfo[] houseInfos)
        {
            matchStatistics = new MatchStatistics(ProgramConstants.GAME_VERSION, UniqueGameID,
                Map.Name, GameMode.UIName, Players.Count, Map.IsCoop);

            bool isValidForStar = true;
            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
            {
                if ((checkBox.MapScoringMode == CheckBoxMapScoringMode.DenyWhenChecked && checkBox.Checked) ||
                    (checkBox.MapScoringMode == CheckBoxMapScoringMode.DenyWhenUnchecked && !checkBox.Checked))
                {
                    isValidForStar = false;
                    break;
                }
            }

            matchStatistics.IsValidForStar = isValidForStar;

            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];
                matchStatistics.AddPlayer(pInfo.Name, pInfo.Name == ProgramConstants.PLAYERNAME,
                    false, pInfo.SideId == SideCount + RandomSelectorCount, houseInfos[pId].SideIndex + 1, pInfo.TeamId,
                    MPColors.FindIndex(c => c.GameColorIndex == houseInfos[pId].ColorIndex), 10);
            }

            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                var pHouseInfo = houseInfos[Players.Count + aiId];
                PlayerInfo aiInfo = AIPlayers[aiId];
                matchStatistics.AddPlayer("Computer", false, true, false,
                    pHouseInfo.SideIndex + 1, aiInfo.TeamId,
                    MPColors.FindIndex(c => c.GameColorIndex == pHouseInfo.ColorIndex),
                    aiInfo.AILevel);
            }
        }

        /// <summary>
        /// Writes spawnmap.ini.
        /// </summary>
        private void WriteMap(PlayerHouseInfo[] houseInfos)
        {
            FileInfo spawnMapIniFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNMAP_INI);

            DeleteSupplementalMapFiles();
            spawnMapIniFile.Delete();

            Logger.Log("Writing map.");

            Logger.Log("Loading map INI from " + Map.CompleteFilePath);

            IniFile mapIni = Map.GetMapIni();

            if (!chkExtension.Checked && ((Mod)cmbGame.SelectedItem.Tag).md == string.Empty)
            {
                IniFile.ConsolidateIniFiles(mapIni, new IniFile("Resources/rules_repair_ra2.ini"));
                IniFile.ConsolidateIniFiles(mapIni, new IniFile("Resources/repair_rules_ra2.ini"));
            }

            IniFile globalCodeIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "INI", "MapCode", "GlobalCode.ini"));

            MapCodeHelper.ApplyMapCode(mapIni, GameMode.GetMapRulesIniFile());
            MapCodeHelper.ApplyMapCode(mapIni, globalCodeIni);

            if (isMultiplayer)
            {
                IniFile mpGlobalCodeIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "INI", "MapCode", "MultiplayerGlobalCode.ini"));
                MapCodeHelper.ApplyMapCode(mapIni, mpGlobalCodeIni);
            }

            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
                checkBox.ApplyMapCode(mapIni, GameMode);

            foreach (GameLobbyDropDown dropDown in DropDowns)
                dropDown.ApplyMapCode(mapIni, GameMode);

            mapIni.MoveSectionToFirst("MultiplayerDialogSettings"); // Required by YR

            // 把最大数量改成100
            if (!mapIni.SectionExists("General"))
                mapIni.AddSection("General");
            if (mapIni.GetIntValue("General", "MaximumQueuedObjects", 0) == 0)
                mapIni.SetIntValue("General", "MaximumQueuedObjects", 100);


            IniFile.ConsolidateIniFiles(mapIni, new IniFile("Client/custom_rules_all.ini"));
            IniFile.ConsolidateIniFiles(mapIni, new IniFile("Resources/SkinRulesmd.ini"));

            string mapIniFileName = Path.GetFileName(mapIni.FileName);
            mapIni.SetStringValue("Basic", "OriginalFilename", mapIniFileName);
            CopySupplementalMapFiles(mapIni);

            ManipulateStartingLocations(mapIni, houseInfos);

            mapIni.WriteIniFile(spawnMapIniFile.FullName);

            //if (chkExtension.Checked)
            //{
            //    var inifile = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawnmap.ini"));
            //    inifile.RenameSection("Countries", "YBCountry");
            //    inifile.Save();
            //}
        }

        /// <summary>
        /// Some mods require that .map files also have supplemental files copied over with the spawnmap.ini.
        /// 
        /// This function scans the directory containing the map file and looks for other files with the
        /// same base filename as the map file that are allowed by the client configuration.
        /// Those files are then copied to the game base path with the base filename of "spawnmap.EXT".
        /// </summary>
        /// <param name="mapIni"></param>
        private void CopySupplementalMapFiles(IniFile mapIni)
        {
            var mapFileInfo = new FileInfo(mapIni.FileName);
            string mapFileBaseName = Path.GetFileNameWithoutExtension(mapFileInfo.Name);

            IEnumerable<string> supplementalMapFiles = GetSupplementalMapFiles(mapFileInfo.DirectoryName, mapFileBaseName).ToList();
            if (!supplementalMapFiles.Any())
                return;

            List<string> supplementalFileNames = new();
            foreach (string file in supplementalMapFiles)
            {
                try
                {
                    // Copy each supplemental file
                    string supplementalFileName = $"spawnmap{Path.GetExtension(file)}";
                    File.Copy(file, SafePath.CombineFilePath(ProgramConstants.GamePath, supplementalFileName), true);
                    supplementalFileNames.Add(supplementalFileName);
                }
                catch (Exception e)
                {
                    string errorMessage = "Unable to copy supplemental map file".L10N("UI:Main:SupplementalFileCopyError") + $" {file}";
                    Logger.Log(errorMessage);
                    Logger.Log(e.Message);
                    XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), errorMessage);

                }
            }

            // Write the supplemental map files to the INI (eventual spawnmap.ini)
            mapIni.SetStringValue("Basic", "SupplementalFiles", string.Join(',', supplementalFileNames));
        }

        /// <summary>
        /// Delete all supplemental map files from last spawn
        /// </summary>
        private void DeleteSupplementalMapFiles()
        {
            IEnumerable<string> supplementalMapFilePaths = GetSupplementalMapFiles(ProgramConstants.GamePath, "spawnmap").ToList();
            if (!supplementalMapFilePaths.Any())
                return;

            foreach (string supplementalMapFilename in supplementalMapFilePaths)
            {
                try
                {
                    File.Delete(supplementalMapFilename);
                }
                catch (Exception e)
                {
                    string errorMessage = "Unable to delete supplemental map file".L10N("UI:Main:SupplementalFileDeleteError") + $" {supplementalMapFilename}";
                    Logger.Log(errorMessage);
                    Logger.Log(e.Message);
                    XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), errorMessage);
                }
            }
        }

        private static IEnumerable<string> GetSupplementalMapFiles(string basePath, string baseFileName)
        {
            // Get the supplemental file names for allowable extensions
            var supplementalMapFileNames = ClientConfiguration.Instance.SupplementalMapFileExtensions
                .Select(ext => $"{baseFileName}.{ext}".ToUpperInvariant())
                .ToList();

            if (!supplementalMapFileNames.Any())
                return new List<string>();

            // Get full file paths for all possible supplemental files
            return Directory.GetFiles(basePath, $"{baseFileName}.*")
                .Where(f => supplementalMapFileNames.Contains(Path.GetFileName(f).ToUpperInvariant()));
        }

        private void ManipulateStartingLocations(IniFile mapIni, PlayerHouseInfo[] houseInfos)
        {
            if (RemoveStartingLocations)
            {
                if (Map.EnforceMaxPlayers)
                    return;

                // All random starting locations given by the game
                IniSection waypointSection = mapIni.GetSection("Waypoints");
                if (waypointSection == null)
                    return;

                
                for (int i = 0; i <= 7; i++)
                {
                    int index = waypointSection.Keys.FindIndex(k => !string.IsNullOrEmpty(k.Key) && k.Key == i.ToString());
                    if (index > -1)
                        waypointSection.Keys.RemoveAt(index);
                }
            }

            // Multiple players cannot properly share the same starting location
            // without breaking the SpawnX house logic that pre-placed objects depend on

            // To work around this, we add new starting locations that just point
            // to the same cell coordinates as existing stacked starting locations
            // and make additional players in the same start loc start from the new
            // starting locations instead.

            // As an additional restriction, players can only start from waypoints 0 to 7.
            // That means that if the map already has too many starting waypoints,
            // we need to move existing (but un-occupied) starting waypoints to point
            // to the stacked locations so we can spawn the players there.


            // Check for stacked starting locations (locations with more than 1 player on it)
            bool[] startingLocationUsed = new bool[MAX_PLAYER_COUNT];
            bool stackedStartingLocations = false;
            foreach (PlayerHouseInfo houseInfo in houseInfos)
            {
                if (houseInfo.RealStartingWaypoint > -1)
                {
                    startingLocationUsed[houseInfo.RealStartingWaypoint] = true;

                    // If assigned starting waypoint is unknown while the real
                    // starting location is known, it means that
                    // the location is shared with another player
                    if (houseInfo.StartingWaypoint == -1)
                    {
                        stackedStartingLocations = true;
                    }
                }
            }

            // If any starting location is stacked, re-arrange all starting locations
            // so that unused starting locations are removed and made to point at used
            // starting locations
            if (!stackedStartingLocations)
                return;

            // We also need to modify spawn.ini because WriteSpawnIni
            // doesn't handle stacked positions.
            // We could move this code there, but then we'd have to process
            // the stacked locations in two places (here and in WriteSpawnIni)
            // because we'd need to modify the map anyway.
            // Not sure whether having it like this or in WriteSpawnIni
            // is better, but this implementation is quicker to write for now.
            IniFile spawnIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS));

            // For each player, check if they're sharing the starting location
            // with someone else
            // If they are, find an unused waypoint and assign their
            // starting location to match that
            for (int pId = 0; pId < houseInfos.Length; pId++)
            {
                PlayerHouseInfo houseInfo = houseInfos[pId];

                if (houseInfo.RealStartingWaypoint > -1 &&
                    houseInfo.StartingWaypoint == -1)
                {
                    // Find first unused starting location index
                    int unusedLocation = -1;
                    for (int i = 0; i < startingLocationUsed.Length; i++)
                    {
                        if (!startingLocationUsed[i])
                        {
                            unusedLocation = i;
                            startingLocationUsed[i] = true;
                            break;
                        }
                    }

                    houseInfo.StartingWaypoint = unusedLocation;
                    mapIni.SetIntValue("Waypoints", unusedLocation.ToString(),
                        mapIni.GetIntValue("Waypoints", houseInfo.RealStartingWaypoint.ToString(), 0));
                    spawnIni.SetIntValue("SpawnLocations", $"Multi{pId + 1}", unusedLocation);
                }
            }

            spawnIni.WriteIniFile();
        }


        /// <summary>
        /// Writes spawn.ini, writes the map file, initializes statistics and
        /// starts the game process.
        /// </summary>
        protected virtual void StartGame()
        {
            PlayerHouseInfo[] houseInfos = [];


            houseInfos = WriteSpawnIni();

            if (houseInfos == null)
                return;

            InitializeMatchStatistics(houseInfos);
            WriteMap(houseInfos);

            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;

            GameProcessLogic.StartGameProcess(WindowManager);
            UpdateDiscordPresence(true);
        }

        private void GameProcessExited_Callback() => AddCallback(new Action(GameProcessExited), null);

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;

            Logger.Log("GameProcessExited: Parsing statistics.");

            matchStatistics.ParseStatistics(ProgramConstants.GamePath, ClientConfiguration.Instance.LocalGame, false);

            Logger.Log("GameProcessExited: Adding match to statistics.");

            StatisticsManager.Instance.AddMatchAndSaveDatabase(true, matchStatistics);

            ClearReadyStatuses();

            CopyPlayerDataToUI();

            UpdateDiscordPresence(true);

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

        /// <summary>
        /// "Copies" player information from the UI to internal memory,
        /// applying users' player options changes.
        /// </summary>
        protected virtual void CopyPlayerDataFromUI(object sender, EventArgs e)
        {
            if (PlayerUpdatingInProgress)
                return;

            var senderDropDown = (XNADropDown)sender;
            if ((bool)senderDropDown.Tag)
                ClearReadyStatuses();

            var oldSideId = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME)?.SideId;

            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];

                pInfo.ColorId = ddPlayerColors[pId].SelectedIndex;
                pInfo.SideId = ddPlayerSides[pId].SelectedIndex;
                pInfo.StartingLocation = ddPlayerStarts[pId].SelectedIndex;
                pInfo.TeamId = ddPlayerTeams[pId].SelectedIndex;

                if (pInfo.SideId == SideCount + RandomSelectorCount)
                    pInfo.StartingLocation = 0;

                XNADropDown ddName = ddPlayerNames[pId];

                switch (ddName.SelectedIndex)
                {
                    case 0:
                        break;
                    case 1:
                        ddName.SelectedIndex = 0;
                        break;
                    case 2:
                        KickPlayer(pId);
                        break;
                    case 3:
                        BanPlayer(pId);
                        break;
                }
            }

            AIPlayers.Clear();
            for (int cmbId = Players.Count; cmbId < 8; cmbId++)
            {
                XNADropDown dd = ddPlayerNames[cmbId];
                dd.Items[0].Text = "-";

                if (dd.SelectedIndex < 1)
                    continue;

                PlayerInfo aiPlayer = new PlayerInfo
                {
                    Name = dd.Items[dd.SelectedIndex].Text,
                    AILevel = dd.SelectedIndex - 1,
                    SideId = Math.Max(ddPlayerSides[cmbId].SelectedIndex, 0),
                    ColorId = Math.Max(ddPlayerColors[cmbId].SelectedIndex, 0),
                    StartingLocation = Math.Max(ddPlayerStarts[cmbId].SelectedIndex, 0),
                    TeamId = Map != null && Map.IsCoop ? 1 : Math.Max(ddPlayerTeams[cmbId].SelectedIndex, 0),
                    IsAI = true
                };

                AIPlayers.Add(aiPlayer);
            }

            CopyPlayerDataToUI();
            btnLaunchGame.SetRank(GetRank());

            if (oldSideId != Players.Find(p => p.Name == ProgramConstants.PLAYERNAME)?.SideId)
                UpdateDiscordPresence();
        }

        /// <summary>
        /// Sets the ready status of all non-host human players to false.
        /// </summary>
        /// <param name="resetAutoReady">If set, players with autoready enabled are reset as well.</param>
        protected void ClearReadyStatuses(bool resetAutoReady = false)
        {
            for (int i = 1; i < Players.Count; i++)
            {
                if (resetAutoReady || !Players[i].AutoReady || Players[i].IsInGame)
                    Players[i].Ready = false;
            }
        }

        private bool CanRightClickMultiplayer(XNADropDownItem selectedPlayer)
        {
            return selectedPlayer != null &&
                   selectedPlayer.Text != ProgramConstants.PLAYERNAME &&
                   !ProgramConstants.AI_PLAYER_NAMES.Contains(selectedPlayer.Text);
        }

        private void MultiplayerName_RightClick(object sender, EventArgs e)
        {
            var selectedPlayer = ((XNADropDown)sender).SelectedItem;
            if (!CanRightClickMultiplayer(selectedPlayer))
                return;

            if (selectedPlayer == null ||
                selectedPlayer.Text == ProgramConstants.PLAYERNAME)
            {
                return;
            }

            MultiplayerNameRightClicked?.Invoke(this, new MultiplayerNameRightClickedEventArgs(selectedPlayer.Text));
        }

        /// <summary>
        /// Applies player information changes done in memory to the UI.
        /// </summary>
        protected virtual void CopyPlayerDataToUI()
        {
            PlayerUpdatingInProgress = true;

            bool allowOptionsChange = AllowPlayerOptionsChange();
            var playerExtraOptions = GetPlayerExtraOptions();

            // Human players
            for (int pId = 0; pId < Players.Count; pId++)
            {
                PlayerInfo pInfo = Players[pId];

                pInfo.Index = pId;

                XNADropDown ddPlayerName = ddPlayerNames[pId];
                ddPlayerName.Items[0].Text = pInfo.Name;
                ddPlayerName.Items[1].Text = string.Empty;
                ddPlayerName.Items[2].Text = "Kick".L10N("UI:Main:Kick");
                ddPlayerName.Items[3].Text = "Ban".L10N("UI:Main:Ban");
                ddPlayerName.SelectedIndex = 0;
                ddPlayerName.AllowDropDown = false;

                bool allowPlayerOptionsChange = allowOptionsChange || pInfo.Name == ProgramConstants.PLAYERNAME;

                ddPlayerSides[pId].SelectedIndex = pInfo.SideId;
                ddPlayerSides[pId].AllowDropDown = !playerExtraOptions.IsForceRandomSides && allowPlayerOptionsChange;

                ddPlayerColors[pId].SelectedIndex = pInfo.ColorId;
                ddPlayerColors[pId].AllowDropDown = !playerExtraOptions.IsForceRandomColors && allowPlayerOptionsChange;

                ddPlayerStarts[pId].SelectedIndex = pInfo.StartingLocation;

                ddPlayerTeams[pId].SelectedIndex = pInfo.TeamId;
                if (GameModeMap != null)
                {
                    ddPlayerTeams[pId].AllowDropDown = !playerExtraOptions.IsForceRandomTeams && allowPlayerOptionsChange && !Map.IsCoop && !Map.ForceNoTeams && !GameMode.ForceNoTeams;
                    ddPlayerStarts[pId].AllowDropDown = !playerExtraOptions.IsForceRandomStarts && allowPlayerOptionsChange && (Map.IsCoop || !Map.ForceRandomStartLocations && !GameMode.ForceRandomStartLocations);
                }
            }

            // AI players
            for (int aiId = 0; aiId < AIPlayers.Count; aiId++)
            {
                PlayerInfo aiInfo = AIPlayers[aiId];

                int index = Players.Count + aiId;

                aiInfo.Index = index;

                XNADropDown ddPlayerName = ddPlayerNames[index];
                ddPlayerName.Items[0].Text = "-";
                ddPlayerName.Items[1].Text = ProgramConstants.AI_PLAYER_NAMES[0];
                ddPlayerName.Items[2].Text = ProgramConstants.AI_PLAYER_NAMES[1];
                ddPlayerName.Items[3].Text = ProgramConstants.AI_PLAYER_NAMES[2];
                ddPlayerName.SelectedIndex = 1 + aiInfo.AILevel;
                ddPlayerName.AllowDropDown = allowOptionsChange;

                ddPlayerSides[index].SelectedIndex = aiInfo.SideId;
                ddPlayerSides[index].AllowDropDown = !playerExtraOptions.IsForceRandomSides && allowOptionsChange;

                ddPlayerColors[index].SelectedIndex = aiInfo.ColorId;
                ddPlayerColors[index].AllowDropDown = !playerExtraOptions.IsForceRandomColors && allowOptionsChange;

                ddPlayerStarts[index].SelectedIndex = aiInfo.StartingLocation;

                ddPlayerTeams[index].SelectedIndex = aiInfo.TeamId;

                if (GameModeMap != null)
                {
                    ddPlayerTeams[index].AllowDropDown = !playerExtraOptions.IsForceRandomTeams && allowOptionsChange && !Map.IsCoop && !Map.ForceNoTeams && !GameMode.ForceNoTeams;
                    ddPlayerStarts[index].AllowDropDown = !playerExtraOptions.IsForceRandomStarts && allowOptionsChange && (Map.IsCoop || !Map.ForceRandomStartLocations && !GameMode.ForceRandomStartLocations);
                }
            }

            // Unused player slots
            for (int ddIndex = Players.Count + AIPlayers.Count; ddIndex < MAX_PLAYER_COUNT; ddIndex++)
            {
                XNADropDown ddPlayerName = ddPlayerNames[ddIndex];
                ddPlayerName.AllowDropDown = false;
                ddPlayerName.Items[0].Text = string.Empty;
                ddPlayerName.Items[1].Text = ProgramConstants.AI_PLAYER_NAMES[0];
                ddPlayerName.Items[2].Text = ProgramConstants.AI_PLAYER_NAMES[1];
                ddPlayerName.Items[3].Text = ProgramConstants.AI_PLAYER_NAMES[2];
                ddPlayerName.SelectedIndex = 0;

                ddPlayerSides[ddIndex].SelectedIndex = -1;
                ddPlayerSides[ddIndex].AllowDropDown = false;

                ddPlayerColors[ddIndex].SelectedIndex = -1;
                ddPlayerColors[ddIndex].AllowDropDown = false;

                ddPlayerStarts[ddIndex].SelectedIndex = -1;
                ddPlayerStarts[ddIndex].AllowDropDown = false;

                ddPlayerTeams[ddIndex].SelectedIndex = -1;
                ddPlayerTeams[ddIndex].AllowDropDown = false;
            }

            if (allowOptionsChange && Players.Count + AIPlayers.Count < MAX_PLAYER_COUNT)
                ddPlayerNames[Players.Count + AIPlayers.Count].AllowDropDown = true;

            MapPreviewBox.UpdateStartingLocationTexts();
            UpdateMapPreviewBoxEnabledStatus();

            PlayerUpdatingInProgress = false;
        }

        /// <summary>
        /// Updates the enabled status of starting location selectors
        /// in the map preview box.
        /// </summary>
        protected abstract void UpdateMapPreviewBoxEnabledStatus();

        /// <summary>
        /// Override this in a derived class to kick players.
        /// </summary>
        /// <param name="playerIndex">The index of the player that should be kicked.</param>
        protected virtual void KickPlayer(int playerIndex)
        {
            // Do nothing by default
        }

        /// <summary>
        /// Override this in a derived class to ban players.
        /// </summary>
        /// <param name="playerIndex">The index of the player that should be banned.</param>
        protected virtual void BanPlayer(int playerIndex)
        {
            // Do nothing by default
        }

        /// <summary>
        /// Changes the current map and game mode.
        /// </summary>
        /// <param name="gameModeMap">The new game mode map.</param>
        protected virtual void ChangeMap(GameModeMap gameModeMap)
        {
            GameModeMap = gameModeMap;

            if (GameMode == null || Map == null)
            {
                lblMapName.Text = "Map: Unknown".L10N("UI:Main:MapUnknown");
                lblMapAuthor.Text = "By Unknown Author".L10N("UI:Main:AuthorByUnknown");
                lblGameMode.Text = "Game mode: Unknown".L10N("UI:Main:GameModeUnknown");
                lblMapSize.Text = "Size: Not available".L10N("UI:Main:MapSizeUnknown");

                MapPreviewBox.GameModeMap = null;

                return;
            }

            lblMapName.Text = "Map:".L10N("UI:Main:Map") + " " + Renderer.GetSafeString(Map.Name, lblMapName.FontIndex);
            lblMapAuthor.Text = "By".L10N("UI:Main:AuthorBy") + " " + Renderer.GetSafeString(Map.Author, lblMapAuthor.FontIndex);
            lblGameMode.Text = "Game mode:".L10N("UI:Main:GameModeLabel") + " " + GameMode.UIName;
            lblMapSize.Text = "Size:".L10N("UI:Main:MapSize") + " " + Map.GetSizeString();

            disableGameOptionUpdateBroadcast = true;

            // Clear forced options
            foreach (var ddGameOption in DropDowns)
                ddGameOption.AllowDropDown = true;

            foreach (var checkBox in CheckBoxes)
                checkBox.AllowChecking = true;

            // We could either pass the CheckBoxes and DropDowns of this class
            // to the Map and GameMode instances and let them apply their forced
            // options, or we could do it in this class with helper functions.
            // The second approach is probably clearer.

            // We use these temp lists to determine which options WERE NOT forced
            // by the map. We then return these to user-defined settings.
            // This prevents forced options from one map getting carried
            // to other maps.

            var checkBoxListClone = new List<GameLobbyCheckBox>(CheckBoxes);
            var dropDownListClone = new List<GameLobbyDropDown>(DropDowns);

            List<KeyValuePair<string, bool>> CheckBoxeResult = CheckBoxes
           .Where(checkBox => checkBox.Special && GameMode.Special) // 仅选择 Special 为 true 的项
           .Select(checkBox => new KeyValuePair<string, bool>(checkBox.Name, false)) // 转换为 KeyValuePair
           .ToList();

            ApplyForcedCheckBoxOptions(checkBoxListClone, GameMode.ForcedCheckBoxValues);
            ApplyForcedCheckBoxOptions(checkBoxListClone, Map.ForcedCheckBoxValues);
            ApplyForcedCheckBoxOptions(checkBoxListClone, CheckBoxeResult);

            List<KeyValuePair<string, int>> DropDownResult = DropDowns
           .Where(dd => dd.Special && GameMode.Special) // 仅选择 Special 为 true 的项
           .Select(dd => new KeyValuePair<string, int>(dd.Name, dd.defaultIndex)) // 转换为 KeyValuePair
           .ToList();

            ApplyForcedDropDownOptions(dropDownListClone, GameMode.ForcedDropDownValues);
            ApplyForcedDropDownOptions(dropDownListClone, Map.ForcedDropDownValues);
            ApplyForcedDropDownOptions(dropDownListClone, DropDownResult);


            foreach (var chkBox in checkBoxListClone)
                chkBox.Checked = chkBox.HostChecked;

            foreach (var dd in dropDownListClone)
                dd.SelectedIndex = dd.HostSelectedIndex;

            // Enable all sides by default
            foreach (var ddSide in ddPlayerSides)
            {
                ddSide.Items.ForEach(item => item.Selectable = true);
            }

            // Enable all colors by default
            foreach (var ddColor in ddPlayerColors)
            {
                ddColor.Items.ForEach(item => item.Selectable = true);
            }

            // Apply starting locations
            foreach (var ddStart in ddPlayerStarts)
            {
                ddStart.Items.Clear();

                ddStart.AddItem("???");

                for (int i = 1; i <= Map.MaxPlayers; i++)
                    ddStart.AddItem(i.ToString());
            }


            // Check if AI players allowed
            bool AIAllowed = !(Map.MultiplayerOnly || GameMode.MultiplayerOnly) ||
                             !(Map.HumanPlayersOnly || GameMode.HumanPlayersOnly);
            foreach (var ddName in ddPlayerNames)
            {
                if (ddName.Items.Count > 3)
                {
                    ddName.Items[1].Selectable = AIAllowed;
                    ddName.Items[2].Selectable = AIAllowed;
                    ddName.Items[3].Selectable = AIAllowed;
                }
            }

            if (!AIAllowed) AIPlayers.Clear();
            IEnumerable<PlayerInfo> concatPlayerList = Players.Concat(AIPlayers).ToList();

            foreach (PlayerInfo pInfo in concatPlayerList)
            {
                if (pInfo.StartingLocation > Map.MaxPlayers ||
                    (!Map.IsCoop && (Map.ForceRandomStartLocations || GameMode.ForceRandomStartLocations)))
                    pInfo.StartingLocation = 0;
                if (!Map.IsCoop && (Map.ForceNoTeams || GameMode.ForceNoTeams))
                    pInfo.TeamId = 0;
            }

            CheckDisallowedSides();


            if (Map.CoopInfo != null)
            {
                // Co-Op map disallowed color logic
                foreach (int disallowedColorIndex in Map.CoopInfo.DisallowedPlayerColors)
                {
                    if (disallowedColorIndex >= MPColors.Count)
                        continue;

                    foreach (XNADropDown ddColor in ddPlayerColors)
                        ddColor.Items[disallowedColorIndex + 1].Selectable = false;

                    foreach (PlayerInfo pInfo in concatPlayerList)
                    {
                        if (pInfo.ColorId == disallowedColorIndex + 1)
                            pInfo.ColorId = 0;
                    }
                }

                // Force teams
                foreach (PlayerInfo pInfo in concatPlayerList)
                    pInfo.TeamId = 1;
            }

            OnGameOptionChanged();

            MapPreviewBox.GameModeMap = GameModeMap;
            CopyPlayerDataToUI();

            disableGameOptionUpdateBroadcast = false;

            PlayerExtraOptionsPanel?.UpdateForMap(Map);
        }

        private void ApplyForcedCheckBoxOptions(List<GameLobbyCheckBox> optionList,
            List<KeyValuePair<string, bool>> forcedOptions)
        {
            foreach (KeyValuePair<string, bool> option in forcedOptions)
            {
                GameLobbyCheckBox checkBox = CheckBoxes.Find(chk => chk.Name == option.Key);

                if (checkBox != null)
                {

                    checkBox.Checked = option.Value;
                    checkBox.AllowChecking = false;
                    optionList.Remove(checkBox);
                }
            }
        }

        private void ApplyForcedDropDownOptions(List<GameLobbyDropDown> optionList,
            List<KeyValuePair<string, int>> forcedOptions)
        {
            foreach (KeyValuePair<string, int> option in forcedOptions)
            {
                GameLobbyDropDown dropDown = DropDowns.Find(dd => dd.Name == option.Key);
                if (dropDown != null)
                {
                    dropDown.SelectedIndex = option.Value;
                    dropDown.AllowDropDown = false;
                    optionList.Remove(dropDown);
                }
            }
        }

        protected string AILevelToName(int aiLevel)
        {
            return ProgramConstants.GetAILevelName(aiLevel);
        }

        protected GameType GetGameType()
        {
            int teamCount = GetPvPTeamCount();

            if (teamCount == 0)
                return GameType.FFA;

            if (teamCount == 1)
                return GameType.Coop;

            return GameType.TeamGame;
        }

        protected int GetRank()
        {
            if (GameMode == null || Map == null)
                return RANK_NONE;

            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
            {
                if ((checkBox.MapScoringMode == CheckBoxMapScoringMode.DenyWhenChecked && checkBox.Checked) ||
                    (checkBox.MapScoringMode == CheckBoxMapScoringMode.DenyWhenUnchecked && !checkBox.Checked))
                {
                    return RANK_NONE;
                }
            }

            PlayerInfo localPlayer = Players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

            if (localPlayer == null)
                return RANK_NONE;

            if (IsPlayerSpectator(localPlayer))
                return RANK_NONE;

            // These variables are used by both the skirmish and multiplayer code paths
            int[] teamMemberCounts = new int[5];
            int lowestEnemyAILevel = 2;
            int highestAllyAILevel = 0;

            foreach (PlayerInfo aiPlayer in AIPlayers)
            {
                teamMemberCounts[aiPlayer.TeamId]++;

                if (aiPlayer.TeamId > 0 && aiPlayer.TeamId == localPlayer.TeamId)
                {
                    if (aiPlayer.AILevel > highestAllyAILevel)
                        highestAllyAILevel = aiPlayer.AILevel;
                }
                else
                {
                    if (aiPlayer.AILevel < lowestEnemyAILevel)
                        lowestEnemyAILevel = aiPlayer.AILevel;
                }
            }

            if (isMultiplayer)
            {
                if (Players.Count == 1)
                    return RANK_NONE;

                // PvP stars for 2-player and 3-player maps
                if (Map.MaxPlayers <= 3)
                {
                    List<PlayerInfo> filteredPlayers = Players.Where(p => !IsPlayerSpectator(p)).ToList();

                    if (AIPlayers.Count > 0)
                        return RANK_NONE;

                    if (filteredPlayers.Count != Map.MaxPlayers)
                        return RANK_NONE;

                    int localTeamIndex = localPlayer.TeamId;
                    if (localTeamIndex > 0 && filteredPlayers.Count(p => p.TeamId == localTeamIndex) > 1)
                        return RANK_NONE;

                    return RANK_HARD;
                }

                // Coop stars for maps with 4 or more players
                // See the code in StatisticsManager.GetRankForCoopMatch for the conditions

                if (Players.Find(p => IsPlayerSpectator(p)) != null)
                    return RANK_NONE;

                if (AIPlayers.Count == 0)
                    return RANK_NONE;

                if (Players.Find(p => p.TeamId != localPlayer.TeamId) != null)
                    return RANK_NONE;

                if (Players.Find(p => p.TeamId == 0) != null)
                    return RANK_NONE;

                if (AIPlayers.Find(p => p.TeamId == 0) != null)
                    return RANK_NONE;

                teamMemberCounts[localPlayer.TeamId] += Players.Count;

                if (lowestEnemyAILevel < highestAllyAILevel)
                {
                    // Check that the player's AI allies aren't stronger
                    return RANK_NONE;
                }

                // Check that all teams have at least as many players
                // as the human players' team
                int allyCount = teamMemberCounts[localPlayer.TeamId];

                for (int i = 1; i < 5; i++)
                {
                    if (i == localPlayer.TeamId)
                        continue;

                    if (teamMemberCounts[i] > 0)
                    {
                        if (teamMemberCounts[i] < allyCount)
                            return RANK_NONE;
                    }
                }

                return lowestEnemyAILevel + 1;
            }

            // *********
            // Skirmish!
            // *********

            if (AIPlayers.Count != Map.MaxPlayers - 1)
                return RANK_NONE;

            teamMemberCounts[localPlayer.TeamId]++;

            if (lowestEnemyAILevel < highestAllyAILevel)
            {
                // Check that the player's AI allies aren't stronger
                return RANK_NONE;
            }

            if (localPlayer.TeamId > 0)
            {
                // Check that all teams have at least as many players
                // as the local player's team
                int allyCount = teamMemberCounts[localPlayer.TeamId];

                for (int i = 1; i < 5; i++)
                {
                    if (i == localPlayer.TeamId)
                        continue;

                    if (teamMemberCounts[i] > 0)
                    {
                        if (teamMemberCounts[i] < allyCount)
                            return RANK_NONE;
                    }
                }

                // Check that there is a team other than the players' team that is at least as large
                bool pass = false;
                for (int i = 1; i < 5; i++)
                {
                    if (i == localPlayer.TeamId)
                        continue;

                    if (teamMemberCounts[i] >= allyCount)
                    {
                        pass = true;
                        break;
                    }
                }

                if (!pass)
                    return RANK_NONE;
            }

            return lowestEnemyAILevel + 1;
        }

        protected string AddGameOptionPreset(string name)
        {
            string error = GameOptionPreset.IsNameValid(name);
            if (!string.IsNullOrEmpty(error))
                return error;

            GameOptionPreset preset = new GameOptionPreset(name);
            foreach (GameLobbyCheckBox checkBox in CheckBoxes)
            {
                try
                {
                    preset.AddCheckBoxValue(checkBox.Name, checkBox.Checked);
                }
                catch
                {
                    continue;
                }
            }

            foreach (GameLobbyDropDown dropDown in DropDowns)
            {
                preset.AddDropDownValue(dropDown.Name, dropDown.SelectedIndex);
            }

            GameOptionPresets.Instance.AddPreset(preset);
            return null;
        }

        public bool LoadGameOptionPreset(string name)
        {
            GameOptionPreset preset = GameOptionPresets.Instance.GetPreset(name);
            if (preset == null)
                return false;

            disableGameOptionUpdateBroadcast = true;

            var checkBoxValues = preset.GetCheckBoxValues();
            foreach (var kvp in checkBoxValues)
            {
                GameLobbyCheckBox checkBox = CheckBoxes.Find(c => c.Name == kvp.Key);
                if (checkBox != null && checkBox.AllowChanges && checkBox.AllowChecking)
                    checkBox.Checked = kvp.Value;
            }

            var dropDownValues = preset.GetDropDownValues();
            foreach (var kvp in dropDownValues)
            {
                GameLobbyDropDown dropDown = DropDowns.Find(d => d.Name == kvp.Key);
                if (dropDown != null && dropDown.AllowDropDown)
                    dropDown.SelectedIndex = kvp.Value;
            }

            disableGameOptionUpdateBroadcast = false;
            OnGameOptionChanged();
            return true;
        }

        protected abstract bool AllowPlayerOptionsChange();
    }
}
