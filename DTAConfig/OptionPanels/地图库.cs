using ClientCore;
using ClientCore.Entity;
using ClientCore.Settings;
using ClientGUI;
using DTAConfig.Entity;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private int _总页数 = 0;

        private XNALabel lblPage;
        private XNAContextMenu _modMenu; 

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

            mapPanel.DoubleLeftClick += 查看地图详细信息;

            mapPanel.LineHeight = 20;
         //   mapPanel.AddColumn("预览图", 200);
            mapPanel.AddColumn("地图名", 120);
            mapPanel.AddColumn("作者", 120);
            mapPanel.AddColumn("地图类型", 100);
            mapPanel.AddColumn("下载次数", 80);
            mapPanel.AddColumn("评分", 80);
            mapPanel.AddColumn("介绍", 100);
            mapPanel.AddColumn("下载", 100);

            mapPanel.RightClick += (sender, args) =>
            {
                _modMenu.Open(GetCursorPoint());
            };

            _modMenu = new XNAContextMenu(WindowManager);
            _modMenu.Name = nameof(_modMenu);
            _modMenu.Width = 100;

            _modMenu.AddItem(new XNAContextMenuItem
            {
                Text = "刷新",
                SelectAction = Reload
            });

            mapPanel.AddChild(_modMenu);

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
                Text = "1 / 1",
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

        private void 查看地图详细信息(object sender, EventArgs e)
        {
            mapPanel.SelectedIndex = mapPanel.HoveredIndex;
            if (mapPanel.SelectedIndex < 0) return;
            var w = new 地图详细信息界面(WindowManager, 地图列表[mapPanel.SelectedIndex].id, types);
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

        private void 下载地图(int id)
        {
            var r =  NetWorkINISettings.Get<Maps>($"map/getMapInfo?id={id}").Result;
            if(r.Item1 != null)
            {
                File.WriteAllText(Path.Combine(ProgramConstants.GamePath, "Maps\\Multi\\WorkShop", $"{r.Item1.name}.map"), r.Item1.file);
            }
        }

        private void DdType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Reload();
        }

        private List<Maps> 地图列表 = [];

        private async void Reload()
        {
            var search = searchBox.Text.Trim() == "搜索地图名..." ? string.Empty : searchBox.Text.Trim();

            var ts = ddType.SelectedIndex == 0 ? string.Empty : $"{ddType.SelectedIndex - 1}";

            var r = await NetWorkINISettings.Get<Page<Maps>>($"map/getRelMapsByPage?search={search}&types={ts}&maxPlayers=&pageNum={当前页数}&pageSize=10");

            if(r.Item1 == null)
            {
                XNAMessageBox.Show(WindowManager, "错误", r.Item2);
                return;
            }

            总页数 = (int)r.Item1.total;

            mapPanel.ClearItems();
            int i = 0;
            地图列表 = r.Item1.records;
            r.Item1.records.ForEach(map =>
            {
                List<XNAListBoxItem> items = [];
                //var item = new XNAListBoxItem
                //{
                //    Texture = AssetLoader.Base64ToTexture(map.base64)
                //};

                //items.Add(item);
                items.Add(new XNAListBoxItem(map.name));
                items.Add(new XNAListBoxItem(map.author));
                items.Add(new XNAListBoxItem(types[map.type]));
                items.Add(new XNAListBoxItem(map.downCount.ToString()));
                items.Add(new XNAListBoxItem(map.score.ToString()));
                items.Add(new XNAListBoxItem(map.description));
                items.Add(new XNAListBoxItem(string.Empty));
                mapPanel.AddItem(items);

                var btn = new XNAClientButton(WindowManager);
                btn.Width = UIDesignConstants.BUTTON_WIDTH_92;
                btn.X = 804;
                btn.Y = 70 + i * mapPanel.LineHeight;
                btn.Text = "下载";
                btn.LeftClick += (_, _) => { 下载地图(map.id); };

                mapPanel.AddChild(btn);
                i++;
            });
        }

    }

    public class 地图详细信息界面 : XNAWindow
    {

        private Maps map;
        private string[] types;

        public 地图详细信息界面(WindowManager windowManager,int mapID, string[] types) : base(windowManager)
        {
            map = NetWorkINISettings.Get<Maps>($"map/getMapInfo?id={mapID}").Result.Item1;
            this.types = types;
        }

        public override void Initialize()
        {
            var 地图预览图 = new XNATextBlock(WindowManager)
            {
                BackgroundTexture = AssetLoader.Base64ToTexture(map.base64),
                ClientRectangle = new Rectangle(10, 10, 256, 153)
            };

            var 地图名 = new XNALabel(WindowManager)
            {
                Text = "地图名称：",
                ClientRectangle = new Rectangle(地图预览图.Right + 20, 地图预览图.Y, 0, 0)
            };

            var 地图名内容 = new XNALabel(WindowManager)
            {
                Text = map.name,
                ClientRectangle = new Rectangle(400, 10, 200, 30)
            };

            var 地图作者 = new XNALabel(WindowManager)
            {
                Text = "地图作者：",
                ClientRectangle = new Rectangle(地图名.X, 地图名.Bottom + 10, 0, 0)
            };

            var 地图作者内容 = new XNALabel(WindowManager)
            {
                Text = map.author,
                ClientRectangle = new Rectangle(地图名内容.X, 地图名内容.Bottom + 10, 200, 30)
            };

            var 地图类型 = new XNALabel(WindowManager)
            {
                Text = "地图类型：",
                ClientRectangle = new Rectangle(地图作者.X, 地图作者.Bottom + 10, 0, 0)
            };

            var 地图类型内容 = new XNALabel(WindowManager)
            {
                Text = types[map.type],
                ClientRectangle = new Rectangle(地图名内容.X, 地图作者内容.Bottom + 10, 200, 30)
            };

            var 地图评分 = new XNALabel(WindowManager)
            {
                Text = "地图评分：",
                ClientRectangle = new Rectangle(地图类型.X, 地图类型.Bottom + 10, 0, 0)
            };

            var 地图评分内容 = new XNALabel(WindowManager)
            {
                Text = map.score.ToString(),
                ClientRectangle = new Rectangle(地图类型内容.X, 地图类型内容.Bottom + 10, 200, 30)
            };

            var 下载次数 = new XNALabel(WindowManager)
            {
                Text = "下载次数：",
                ClientRectangle = new Rectangle(地图评分.X, 地图评分.Bottom + 10, 0, 0)
            };

            var 下载次数内容 = new XNALabel(WindowManager)
            {
                Text = map.downCount.ToString(),
                ClientRectangle = new Rectangle(地图评分内容.X, 地图评分内容.Bottom + 10, 200, 30)
            };

            var 地图介绍 = new XNALabel(WindowManager)
            {
                Text = map.description,
                ClientRectangle = new Rectangle(地图预览图.X, 地图预览图.Bottom + 10, 0, 0)
            };

            var 下载按钮 = new XNAClientButton(WindowManager)
            {
                Text = "下载",
                ClientRectangle = new Rectangle(地图介绍.X, 地图介绍.Bottom + 10, 100, 30)
            };

            下载按钮.LeftClick += (sender, args) =>
            {
                File.WriteAllText(Path.Combine(ProgramConstants.GamePath, "Maps\\Multi\\WorkShop", $"{map.name}.map"), map.file);
            };

            var 关闭按钮 = new XNAClientButton(WindowManager)
            {
                Text = "关闭",
                ClientRectangle = new Rectangle(地图介绍.X + 200, 地图介绍.Bottom + 10, 100, 30)
            };

            关闭按钮.LeftClick += (sender, args) =>
            {
                Disable();
            };

            AddChild(地图预览图);
            AddChild(地图名);
            AddChild(地图名内容);
            AddChild(地图作者);
            AddChild(地图作者内容);
            AddChild(地图类型);
            AddChild(地图类型内容);
            AddChild(地图评分);
            AddChild(地图评分内容);  
            AddChild(下载次数);
            AddChild(下载次数内容);
            AddChild(地图介绍);
            AddChild(下载按钮);
            AddChild(关闭按钮);
             
            base.Initialize();

            CenterOnParent();
        }
    }
}
