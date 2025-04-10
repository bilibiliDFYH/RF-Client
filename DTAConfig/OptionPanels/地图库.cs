using ClientCore.Entity;
using ClientCore.Settings;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTAConfig.OptionPanels
{
   public class 地图库 : XNAWindow
    {
        private static 地图库 _instance;
        private XNASuggestionTextBox searchBox;
        private XNADropDown ddType;
        private XNAMultiColumnListBox mapPanel;
        private string[] types;
        private static readonly object _lock = new();
        private int _当前页数 = 1;
        private int _总页数 = 1;
        private XNALabel lblPage;

        public int 当前页数
        {
            get => _当前页数;
            set
            {
                if (_当前页数 != value)
                {
                    _当前页数 = value;
                    lblPage.Text = $"{_当前页数} / {_总页数}";
                }
            }
        }

        public int 总页数
        {
            get => _总页数;
            set
            {
                if (_总页数 != value)
                {
                    _总页数 = value;
                    lblPage.Text = $"{_当前页数} / {_总页数}";
                }
            }
        }


        // 私有构造函数，防止外部实例化
        private 地图库(WindowManager windowManager) : base(windowManager)
        {
        }

        // 单例访问点
        public static 地图库 GetInstance(WindowManager windowManager)
        {
            if (_instance == null)
            {
                lock (_lock) // 线程安全
                {
                    _instance ??= new 地图库(windowManager);
                    _instance.Initialize();
                    DarkeningPanel.AddAndInitializeWithControl(windowManager, _instance);
                }
            }
            return _instance;
        }

        public override void Initialize()
        {
            Name = "地图库";
            ClientRectangle = new Rectangle(0, 0, 1000, 700);
            CenterOnParent();

            // 标题
            var titleLabel = new XNALabel(WindowManager)
            {
                Text = "地图库",
                ClientRectangle = new Rectangle(450, 20, 100, 30)
            };

            // 搜索框
            searchBox = new XNASuggestionTextBox(WindowManager)
            {
                ClientRectangle = new Rectangle(50, 60, 200, 25),
                Suggestion = "搜索地图名..."
            };

            var btnSearch = new XNAClientButton(WindowManager)
            {
                Text = "搜索",
                ClientRectangle = new Rectangle(260, 60, UIDesignConstants.BUTTON_WIDTH_75, UIDesignConstants.BUTTON_HEIGHT)
            };
            btnSearch.LeftClick += (sender, args) => { Reload(); };

            ddType = new XNADropDown(WindowManager)
            {
                ClientRectangle = new Rectangle(380, 60, 150, 25),
                Text = "地图类型"
            };  

            // 地图列表容器
            mapPanel = new XNAMultiColumnListBox(WindowManager)
            {
                ClientRectangle = new Rectangle(50, 100, 900, 540),
            //    BackgroundColor = Color.Black * 0.5f // 半透明背景
            };

            mapPanel.LineHeight = 120;
            mapPanel.AddColumn("预览图", 200);
            mapPanel.AddColumn("地图名", 120);
            mapPanel.AddColumn("作者", 120);
            mapPanel.AddColumn("地图类型", 100);
            mapPanel.AddColumn("下载次数", 100);
            mapPanel.AddColumn("评分", 100);
            mapPanel.AddColumn("介绍", 160);
            // mapPanel.AddColumn("下载", 100);

            types = NetWorkINISettings.Get<string>("dict/getValue?section=map&key=type").Result.Item1.Split(',');

            ddType.AddItem("所有");
            foreach (var type in types)
            {
                ddType.AddItem(type);
            }
            ddType.SelectedIndexChanged += DdType_SelectedIndexChanged;
            ddType.SelectedIndex = 0;

            var btnLeft = new XNAButton(WindowManager)
            {
                ClientRectangle = new Rectangle(mapPanel.X, mapPanel.Bottom + 10, 20, 20),
                HoverTexture = AssetLoader.LoadTexture("left.png"),
                IdleTexture = AssetLoader.LoadTexture("left.png"),
            };
            btnLeft.LeftClick += BtnLeft_LeftClick;

            lblPage = new XNALabel(WindowManager)
            {
                Text = "0 / 0",
                ClientRectangle = new Rectangle(btnLeft.Right + 10, btnLeft.Y, 0, 0)
            };

            var btnRight = new XNAButton(WindowManager)
            {
                ClientRectangle = new Rectangle(btnLeft.Right + 60, btnLeft.Y, 20, 20),
                HoverTexture = AssetLoader.LoadTexture("right.png"),
                IdleTexture = AssetLoader.LoadTexture("right.png"),
            };
            btnRight.LeftClick += BtnRight_LeftClick;

            // 关闭按钮
            var closeButton = new XNAClientButton(WindowManager)
            {
                Text = "关闭",
                X = 820,
                Y = 655,
            //    ClientRectangle = new Rectangle(870, 620, 120, 40)
            };
            closeButton.LeftClick += (sender, args) => { Disable(); };

            // 添加控件
            AddChild(titleLabel);
            AddChild(searchBox);
            // AddChild(refreshButton);
            AddChild(mapPanel);
            // AddChild(detailButton);
            AddChild(closeButton);
            AddChild(btnSearch);
            AddChild(ddType);
            AddChild(btnLeft);
            AddChild(btnRight);
            AddChild(lblPage);

            base.Initialize();

            // Reload();
        }

        private void BtnRight_LeftClick(object sender, EventArgs e)
        {
            if (当前页数 > 1)
                当前页数--;

        }

        private void BtnLeft_LeftClick(object sender, EventArgs e)
        {
            if(当前页数 < 总页数)
            当前页数++;
        }

        private void DdType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Reload();
        }

        private async void Reload()
        {
            var r = await NetWorkINISettings.Get<Page<Maps>>($"map/getRelMapsByPage?search={searchBox.Text.Trim()}&types={ddType.SelectedIndex - 2}&maxPlayers=&pageNum={当前页数}&pageSize=10");

            总页数 = (int)r.Item1.total;

            mapPanel.ClearItems();
            r.Item1.records.ForEach(map =>
            {
                List<XNAListBoxItem> items = [];
                var item = new XNAListBoxItem
                {
                    Texture = AssetLoader.Base64ToTexture(map.base64)
                };

                items.Add(item);
                items.Add(new XNAListBoxItem(map.name));
                items.Add(new XNAListBoxItem(map.author));
                items.Add(new XNAListBoxItem(types[map.type]));
                items.Add(new XNAListBoxItem(map.downCount.ToString()));
                items.Add(new XNAListBoxItem(map.score.ToString()));
                items.Add(new XNAListBoxItem(map.description));

                mapPanel.AddItem(items);
            });
        }

    }
}
