using ClientCore.Entity;
using ClientCore.Settings;
using ClientGUI;
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

        private static readonly object _lock = new();

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
            var searchBox = new XNASuggestionTextBox(WindowManager)
            {
                ClientRectangle = new Rectangle(50, 60, 200, 30),
                Suggestion = "搜索地图名..."
            };

            // 刷新按钮
            //var refreshButton = new XNAButton(WindowManager)
            //{
            //    Text = "刷新",
            //    ClientRectangle = new Rectangle(270, 60, 100, 30)
            //};
         //   refreshButton.OnClick += (sender, args) => { LoadMapList(); };

            // 地图列表容器
            var mapPanel = new XNAMultiColumnListBox(WindowManager)
            {
                ClientRectangle = new Rectangle(50, 110, 900, 520),
          //      BackgroundColor = Color.Black * 0.5f // 半透明背景
            };

            mapPanel.LineHeight = 100;
            mapPanel.AddColumn("预览图", 160);
            mapPanel.AddColumn("地图名", 100);
            mapPanel.AddColumn("作者", 100);
            mapPanel.AddColumn("地图类型", 100);
            mapPanel.AddColumn("下载次数", 100);
            mapPanel.AddColumn("评分", 100);
            mapPanel.AddColumn("介绍", 160);
            mapPanel.AddColumn("下载", 100);

            var types = NetWorkINISettings.Get<string>("dict/getValue?section=map&key=type").Result.Item1.Split(',');

            var r = NetWorkINISettings.Get<Page<Maps>>("map/getRelMapsByPage?search=&types=&maxPlayers=&pageNum=1&pageSize=1").Result;

            r.Item1.records.ForEach(map =>
            {
                
                List<XNAListBoxItem> items = [];
                var item = new XNAListBoxItem();
                item.Texture = AssetLoader.Base64ToTexture(map.base64);
               
                items.Add(item);
                items.Add(new XNAListBoxItem(map.name));
                items.Add(new XNAListBoxItem(map.author));
                items.Add(new XNAListBoxItem(types[map.type]));
                items.Add(new XNAListBoxItem(map.downCount.ToString()));
                items.Add(new XNAListBoxItem(map.score.ToString()));
                items.Add(new XNAListBoxItem(map.description));
                items.Add(new XNAListBoxItem(map.description));
                mapPanel.AddItem(items);
            });

            
            // 添加示例地图项
            //for (int i = 0; i < 5; i++)
            //{
            //    var mapItem = CreateMapItem($"地图 {i + 1}", "作者X", i);
            //    mapItem.ClientRectangle = new Rectangle(10, 10 + i * 100, 880, 90);
            //    mapPanel.AddChild(mapItem);
            //}

            // 详情按钮
            var detailButton = new XNAButton(WindowManager)
            {
                Text = "查看详情",
                ClientRectangle = new Rectangle(730, 640, 120, 40)
            };
         //   detailButton.OnClick += (sender, args) => { ShowMapDetails(); };

            // 关闭按钮
            var closeButton = new XNAClientButton(WindowManager)
            {
                Text = "关闭",
                X = 870,
                Y = 640,
            //    ClientRectangle = new Rectangle(870, 620, 120, 40)
            };
        //    closeButton.OnClick += (sender, args) => { Close(); };

            // 添加控件
            AddChild(titleLabel);
            AddChild(searchBox);
         //   AddChild(refreshButton);
            AddChild(mapPanel);
            AddChild(detailButton);
            AddChild(closeButton);

            base.Initialize();
        }


    }
}
