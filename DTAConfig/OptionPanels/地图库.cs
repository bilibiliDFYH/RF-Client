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
        private XNAContextMenu _menu; 

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

            mapPanel.LineHeight = 30;
         //   mapPanel.AddColumn("预览图", 200);
            mapPanel.AddColumn("地图名", 200);
            mapPanel.AddColumn("作者", 160);
            mapPanel.AddColumn("地图类型", 100);
            mapPanel.AddColumn("下载次数", 80);
            mapPanel.AddColumn("评分", 80);
            mapPanel.AddColumn("介绍", 100);
            mapPanel.AddColumn("状态", 80);
            mapPanel.AddColumn("安装", 100);

            mapPanel.RightClick += (sender, args) =>
            {
                _menu.Open(GetCursorPoint());
            };

            _menu = new XNAContextMenu(WindowManager);
            _menu.Name = nameof(_menu);
            _menu.Width = 100;

            _menu.AddItem(new XNAContextMenuItem
            {    
                Text = "刷新", 
                SelectAction = Reload
            });

            AddChild(_menu);

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

        private void 查看地图(int id,bool is安装) 

        {
            var w = new 地图详细信息界面(WindowManager, id, types,is安装);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, w);
        }

        private void DdType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Reload();
        }


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

            r.Item1.records.ForEach(map =>
            {
            List<XNAListBoxItem> items = [];
                //var item = new XNAListBoxItem 
                //{
                //    Texture = AssetLoader.Base64ToTexture(map.base64)
                //};

                //items.Add(item);
                var is安装 = File.Exists(Path.Combine(ProgramConstants.MAP_PATH, $"{map.id}.map"));
                items.Add(new XNAListBoxItem(map.name));
                items.Add(new XNAListBoxItem(map.author));
                items.Add(new XNAListBoxItem(types[map.type]));
                items.Add(new XNAListBoxItem(map.downCount.ToString()));
                items.Add(new XNAListBoxItem(map.score.ToString()));
                items.Add(new XNAListBoxItem(map.description));
                items.Add(new XNAListBoxItem(is安装 ? "已安装" : "未安装"));
                items.Add(new XNAListBoxItem(string.Empty));
                mapPanel.AddItem(items);

                var btn = new XNAClientButton(WindowManager);
                btn.Width = UIDesignConstants.BUTTON_WIDTH_92;
                btn.X = 804;
                btn.Y = 25 + i * mapPanel.LineHeight;
                btn.Text = "查看";
                btn.LeftClick += (_, _) => { 查看地图(map.id, is安装); };

                mapPanel.AddChild(btn);
                i++;
            });
        }

    }

    public class 地图详细信息界面 : XNAWindow
    {

        private Maps map;
        private string[] types;
        private bool is下载;
        private XNAClientButton 下载按钮;

        public 地图详细信息界面(WindowManager windowManager,int mapID, string[] types,bool is下载 = true) : base(windowManager)
        {
            map = NetWorkINISettings.Get<Maps>($"map/getMapInfo?id={mapID}").Result.Item1;
            this.types = types;
            this.is下载 = is下载;
        }

        public override void Initialize()
        {

            ClientRectangle = new Rectangle(0, 0, 550, 300);

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
                ClientRectangle = new Rectangle(380, 10, 0, 0)
            };

            var 地图作者 = new XNALabel(WindowManager)
            {
                Text = "地图作者：",
                ClientRectangle = new Rectangle(地图名.X, 地图名.Bottom + 25, 0, 0)
            };

            var 地图作者内容 = new XNALabel(WindowManager)
            {
                Text = map.author,
                ClientRectangle = new Rectangle(地图名内容.X, 地图名内容.Bottom + 25, 0, 0)
            };

            var 地图类型 = new XNALabel(WindowManager)
            {
                Text = "地图类型：",
                ClientRectangle = new Rectangle(地图作者.X, 地图作者.Bottom + 25, 0, 0)
            };

            var 地图类型内容 = new XNALabel(WindowManager)
            {
                Text = types[map.type],
                ClientRectangle = new Rectangle(地图名内容.X, 地图作者内容.Bottom + 25, 0, 0)
            };

            var 地图评分 = new XNALabel(WindowManager)
            {
                Text = "地图评分：",
                ClientRectangle = new Rectangle(地图类型.X, 地图类型.Bottom + 25, 0, 0)
            };

            var 地图评分内容 = new XNALabel(WindowManager)
            {
                Text = map.score.ToString(),
                ClientRectangle = new Rectangle(地图类型内容.X, 地图类型内容.Bottom + 25, 0, 0)
            };

            var 下载次数 = new XNALabel(WindowManager)
            {
                Text = "下载次数：",
                ClientRectangle = new Rectangle(地图评分.X, 地图评分.Bottom + 25, 0, 0)
            };

            var 下载次数内容 = new XNALabel(WindowManager)
            {
                Text = map.downCount.ToString(),
                ClientRectangle = new Rectangle(地图评分内容.X, 地图评分内容.Bottom + 25, 0, 0)
            };

            var 地图介绍 = new XNALabel(WindowManager)
            {
                Text = map.description,
                ClientRectangle = new Rectangle(地图预览图.X, 地图预览图.Bottom + 25, 0, 0)
            };

            下载按钮 = new XNAClientButton(WindowManager)
            {
                
                ClientRectangle = new Rectangle(地图介绍.X, 地图介绍.Bottom + 70, 100, 30),
                IdleTexture = AssetLoader.LoadTexture("75pxbtn.png"),
                HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png")
            };

            下载按钮.LeftClick += 安装;

            if (is下载)
            {
                下载按钮.Text = "安装";
                下载按钮.LeftClick += 安装;
            }
            else
            {
                下载按钮.Text = "删除";
                下载按钮.LeftClick += 删除;
            }

            var 关闭按钮 = new XNAClientButton(WindowManager)
            {
                Text = "关闭",
                ClientRectangle = new Rectangle(Right - 110, 下载按钮.Y, 100, 30),
                IdleTexture = AssetLoader.LoadTexture("75pxbtn.png"),
                HoverTexture = AssetLoader.LoadTexture("75pxbtn_c.png")
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

        private void 安装(object sender, EventArgs e)
        {
            下载按钮.Enabled = false;
            try
            {
                if (!Directory.Exists(ProgramConstants.MAP_PATH))
                    Directory.CreateDirectory(ProgramConstants.MAP_PATH);
                File.WriteAllText(Path.Combine(ProgramConstants.MAP_PATH, $"{map.id}.map"), map.file);
                下载按钮.Text = "删除";
                下载按钮.LeftClick -= 删除;
                下载按钮.LeftClick -= 安装;
                下载按钮.LeftClick += 删除;
            }
            catch(Exception ex)
            {
                XNAMessageBox.Show(WindowManager, "错误", ex.Message);
            }
          
            下载按钮.Enabled = true;
        }

        private void 删除(object sender, EventArgs e)
        {
            下载按钮.Enabled = false;
            try
            {
                File.Delete(Path.Combine(ProgramConstants.MAP_PATH, $"{map.id}.map"));
                下载按钮.Text = "安装";
                下载按钮.LeftClick -= 删除;
                下载按钮.LeftClick -= 安装;
                下载按钮.LeftClick += 安装;
            }
            catch (Exception ex)
            {
                XNAMessageBox.Show(WindowManager, "错误", ex.Message);
            }

            下载按钮.Enabled = true;
        }
    }
}
