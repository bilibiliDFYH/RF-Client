using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Settings;
using ClientGUI;
using HtmlAgilityPack;
using IniParser;
using IniParser.Model;
using Localization.Tools;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Localization.SevenZip;
using System.Text.RegularExpressions;
using DTAConfig.Entity;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Windows.Forms;
using ClientCore.Entity;

namespace DTAConfig.OptionPanels
{
    class ComponentsPanel : XNAOptionsPanel
    {
        private const string COMPONENTS_FILE = "该用户所有组件";

        private XNAMultiColumnListBox CompList;

        private XNALabel labeltypes;

        private XNAClientDropDown comboBoxtypes;

        private XNAClientButton mainButton;

        private XNALabel lbstatus;

        private XNALabel lbprogress;

        private XNAProgressBar progressBar;

        List<XNAClientButton> installationButtons = new List<XNAClientButton>();

        bool downloadCancelled = false;

        string componentnamePath = Path.Combine(ProgramConstants.GamePath, "Resources", "component");

        private FileIniDataParser iniParser;

        private IniData _locIniData;

        private List<SectionData> buttons = new List<SectionData>();

        /// <summary>
        /// 组件列表数据
        /// </summary>
        private List<Component> _components = [];

        private List<Component> All_components = [];


        /// <summary>
        /// 当前选中的组件
        /// </summary>
        private Component _curComponent = null;

        public bool 需要刷新 = false;


        public ComponentsPanel(WindowManager windowManager, UserINISettings iniSettings) : base(windowManager, iniSettings)
        {
            Name = "ComponentsPanel";
            
        }

        public override void Initialize()
        {
            base.Initialize();

            iniParser = new FileIniDataParser();
            try
            {
                labeltypes = new XNALabel(WindowManager);
                labeltypes.Name = nameof(labeltypes);
                labeltypes.ClientRectangle = new Rectangle(20, 10, 0, 0);
                labeltypes.Text = "筛选";
                AddChild(labeltypes);

                comboBoxtypes = new XNAClientDropDown(WindowManager);
                comboBoxtypes.Name = nameof(comboBoxtypes);
                comboBoxtypes.ClientRectangle = new Rectangle(labeltypes.Right + 10, labeltypes.Top - 3, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
                
                comboBoxtypes.SelectedIndex = 0;
                comboBoxtypes.SelectedIndexChanged += ComboBoxtypes_SelectedIndexChanged;
                AddChild(comboBoxtypes);

                CompList = new XNAMultiColumnListBox(WindowManager);
                CompList.Name = nameof(CompList);
                CompList.ClientRectangle = new Rectangle(labeltypes.Left, labeltypes.Bottom + 10, Width - 40, Height - 75);
                CompList.SelectedIndexChanged += CompList_SelectedChanged;
                CompList.LineHeight = 30;
                CompList.FontIndex = 1;

                CompList.AddColumn("序号", 60)
                    .AddColumn("组件", CompList.Width - 420)
                    .AddColumn("类型", 110)
                    .AddColumn("作者", 90)
                    .AddColumn("版本", 70)
                    .AddColumn("状态", 90);
                AddChild(CompList);

                var _menu = new XNAContextMenu(WindowManager);
                _menu.Name = nameof(_menu);
                _menu.Width = 100;

                _menu.AddItem(new XNAContextMenuItem
                {
                    Text = "刷新",
                    SelectAction = () => {
                        InitialComponets();
                    }
                });

                AddChild(_menu);

                CompList.RightClick += (_, _) => {
                    CompList.SelectedIndex = CompList.HoveredIndex;
                    _menu.Open(GetCursorPoint());
                };

                mainButton = new XNAClientButton(WindowManager);
                mainButton.Name = nameof(mainButton);
                mainButton.ClientRectangle = new Rectangle(Width / 2 - 60, CompList.Bottom + 5, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
                mainButton.LeftClick += Btn_LeftClick;
                AddChild(mainButton);

                lbstatus = new XNALabel(WindowManager)
                {
                    Name = nameof(lbstatus),
                    ClientRectangle = new Rectangle(mainButton.Left - 80, CompList.Bottom + 10, 0, 0),
                    Visible = false 
                };
                AddChild(lbstatus);

                lbprogress = new XNALabel(WindowManager)
                {
                    Name = nameof(lbprogress),
                    ClientRectangle = new Rectangle(mainButton.Right + 20, CompList.Bottom + 10, 0, 0),
                    Visible = false
                };
                AddChild(lbprogress);

                progressBar = new XNAProgressBar(WindowManager);
                progressBar.Name = nameof(progressBar);
                progressBar.Maximum = 100;
                progressBar.ClientRectangle = mainButton.ClientRectangle;
                progressBar.Value = 0;
                progressBar.Tag = false;
                progressBar.Visible = false;
                AddChild(progressBar);

                if (!File.Exists(componentnamePath))
                    File.Create(componentnamePath).Close();
                _locIniData = iniParser.ReadFile(componentnamePath);

                InitialComponets();
                
                if (CompList.ItemCount > 0)
                {
                    CompList.SelectedIndex = 0;
                    comboBoxtypes.SelectedIndex = 0;
                }
                else
                    mainButton.Visible = false;
        }
        catch (Exception ex)
        {
            Logger.Log("组件初始化出错：" + ex);
        }
        }

        private void ComboBoxtypes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxtypes.SelectedIndex < 0 || comboBoxtypes.SelectedIndex > comboBoxtypes.Items.Count)
                return;

            if (null == All_components || 0 == All_components.Count)
            {
                comboBoxtypes.SelectedIndex = 0;
                return;
            }
         
         //   if (0 < comboBoxtypes.SelectedIndex)
          //  {
                CompList.ClearItems();
                
                _components = All_components.FindAll(p => comboBoxtypes.SelectedIndex == 0 || p.type == comboBoxtypes.SelectedIndex - 1);
              
          //  }
            InitialComponetsList(_components);
            CompList_SelectedChanged(null, null);
            CompList.TopIndex = -1;
        }


        private void InitialComponets()
        {
            comboBoxtypes.Items.Clear();
            comboBoxtypes.AddItem("全部");
            Task.Run(async () => {
                var Types = (await NetWorkINISettings.Get<string>("dict/getValue?section=component&key=type")).Item1?.Split(",") ?? [];
                foreach (var item in Types) comboBoxtypes.AddItem(item);
            });
            

            Task.Run(async () =>
            {
#if RELEASE
                All_components = (await NetWorkINISettings.Get<List<Component>>("component/getAuditComponent")).Item1??[];
#else
                All_components = (await NetWorkINISettings.Get<List<Component>>("component/getUnAuditComponent")).Item1??[];
                // All_components = (await NetWorkINISettings.Get<List<Component>>("component/getAuditComponent")).Item1 ?? [];
#endif

                if (All_components.Count > 0)
                {
                    ComboBoxtypes_SelectedIndexChanged(null, null);
                }

            });
        }

        private void InitialComponetsList(List<Component> components)
        {
            if (null == components || null == CompList)
                return;
            CompList.ClearItems();
            int i = 0;
            foreach (var comp in components)
            {
                StateItem item = CheckComponentStatus(comp);
                CompList.AddItem(new XNAListBoxItem[] {
                        new(i.ToString()),
                        new (comp.name),
                        new (comp.typeName),
                        new (comp.author),
                        new (comp.version),
                        new (item.Text, item.TextColor)
                    });
                i++;
            }
        }

        /// <summary>
        /// 检测组件包状态 
        /// -1 不可用、0 未安装、1 已安装、2 可更新 
        /// </summary>
        /// <param name="strid"></param>
        /// <returns></returns>
        private StateItem CheckComponentStatus(Component comp)
        {
            StateItem state = new StateItem { Code = -1, Text = "不可用", TextColor = Color.Red };
            string strid = comp.hash;
            if (string.IsNullOrEmpty(strid))
                return state;

            state = new StateItem { Code = 0, Text = "未安装", TextColor = Color.Orange };
            var parser = new FileIniDataParser();
            foreach (SectionData locSec in _locIniData.Sections)
            {
                if (strid == locSec.SectionName)
                {
                    state = new StateItem { Code = 1, Text = "已安装", TextColor = Color.Green };
                    if (CheckVersionNew(locSec.Keys["version"], comp.version))
                    {
                        state = new StateItem { Code = 2, Text = "可更新", TextColor = Color.AliceBlue };
                    }
                    break;
                }
            }
            return state;
        }

        private bool CheckVersionNew(string strLocal, string strServer)
        {
            //先简单判断，版本内容不一样就显示有更新,后续再细化版本号判断
            if(string.IsNullOrEmpty(strLocal) || string.IsNullOrEmpty(strServer))
                return false;
            return strLocal != strServer;
        }

        private void CompList_SelectedChanged(object sender, EventArgs e)
        {
            if (CompList.SelectedIndex < 0 || CompList.SelectedIndex > CompList.ItemCount)
                return;

            if (progressBar != null)
                progressBar.Visible = false;

            RefreshInstallButtonStatus(CompList.SelectedIndex);
        }

        private void RefreshInstallButtonStatus(int nIndex)
        {
            if (nIndex < 0 || nIndex > _components.Count || !this.Visible || _components.Count == 0)
                return;

            _curComponent = _components[nIndex];
            StateItem item = CheckComponentStatus(_curComponent);
            XNAListBoxItem lstItem = CompList.GetItem(5, nIndex);
            if(item.Text != lstItem.Text)
            {
                lstItem.Text = item.Text;
                lstItem.TextColor = item.TextColor;
                CompList.SetItem(5, nIndex, lstItem);
            }

            if (-1 != item.Code)
            {
                mainButton.Visible = true;
                if (0 == item.Code)
                    mainButton.Text = "安装";
                else if (1 == item.Code)
                    mainButton.Text = "卸载";
                else
                    mainButton.Text = "更新";
            }
            else
                mainButton.Visible = false;
        }

        public override void Load()
        {
            base.Load();
        }

        private async Task DownloadfilesAsync()
        {
            if (null == _curComponent)
                return;

            if(null != progressBar)
            {
                mainButton.Visible = false;
                progressBar.Visible = true;
                lbstatus.Visible = true;
                lbprogress.Visible = true;
            }

          
            string strLocPath = string.Empty;

            lbstatus.Text = "正在下载";

            try
            {
                using WebClient webClient = new WebClient();

                webClient.DownloadProgressChanged += (s, evt) =>
                {
                    progressBar.Value = evt.ProgressPercentage;

                    lbprogress.Text = progressBar.Value.ToString() + "%";
                };

                var (strDownPath, message) = (await NetWorkINISettings.Get<string>($"component/getComponentUrl?id={_curComponent.id}"));

                if (string.IsNullOrEmpty(strDownPath))
                {
                    XNAMessageBox.Show(WindowManager, "提示", $"组件包链接获取失败:{message}");
                    return;
                }
                string strTmp = Path.Combine(ProgramConstants.GamePath, "Tmp");
                if (!Directory.Exists(strTmp))
                    Directory.CreateDirectory(strTmp);
                strLocPath = Path.Combine(strTmp, _curComponent.file);

                if (File.Exists(strLocPath))
                    File.Delete(strLocPath);
                await webClient.DownloadFileTaskAsync(new Uri(strDownPath), strLocPath);
            }
                catch (Exception ex)
            {
                Logger.Log(ex.Message);
                if (null != progressBar)
                {
                    mainButton.Visible = true;
                    progressBar.Visible = false;
                    lbstatus.Visible = false;
                    lbprogress.Visible = false;
                }
                RefreshInstallButtonStatus(CompList.SelectedIndex);
            }

            //extract
            if (File.Exists(strLocPath))
            {

                //比对hash，如果远程未设置则不对比
                if(!string.IsNullOrEmpty(_curComponent.hash))
                {
                    //获取文件hash并比对
                    string strfilehash = Utilities.CalculateSHA1ForFile(strLocPath);
                    if (_curComponent.hash != strfilehash)
                    {
                        XNAMessageBox.Show(WindowManager, "错误", $"文件可能被破坏，请重新下载");
                        if (null != progressBar)
                        {
                            mainButton.Visible = true;
                            progressBar.Visible = false;
                            lbstatus.Visible = false;
                            lbprogress.Visible = false;
                        }
                        RefreshInstallButtonStatus(CompList.SelectedIndex);
                        return;
                    }
                }
                lbstatus.Text = "正在解压";
                //安装组件包
                await Task.Run(() =>
                {
                    var TargetPath = "./";
                    if (_curComponent.typeName == "地图" || _curComponent.typeName == "地图包")
                    {
                        TargetPath = "./Maps/Multi/WorkShop/";
                        预写地图配置(_curComponent);
                    }

                    //正则匹配文件路径 类似 Mod&AI\Mod\...\expandmd97.mix 或 *.ini这样的
                    //  string strPattern = "(\\w|&\\w|\\\\)*\\\\(\\w|&\\w|\\\\)*\\.\\w{3}";

                    SevenZip.ExtractWith7Zip(strLocPath, TargetPath, progress =>
                    {
                        lbprogress.Text = $"{progress:0.000}%";
                    });
              
                    WriteConponentConfig(SevenZip.GetFile(strLocPath));
                    try
                    {
                        File.Delete(strLocPath);
                    }catch (Exception ex)
                    {
                        Logger.Log(ex.ToString());
                    }
                });
                mainButton.Text = "卸载";
            }
            else
                mainButton.Text = "安装";

            if (null != progressBar)
            {
                mainButton.Visible = true;
                progressBar.Visible = false;
                lbstatus.Visible = false;
                lbprogress.Visible = false;
            }
            RefreshInstallButtonStatus(CompList.SelectedIndex);
        }

        private void WriteConponentConfig(List<string> files)
        {
            //写本地ini配置，确保可以加载
            _locIniData.Sections.AddSection(_curComponent.hash);
            _locIniData[_curComponent.hash].AddKey("name", _curComponent.name);
            _locIniData[_curComponent.hash].AddKey("version", _curComponent.version);
            StringBuilder sBuff = new StringBuilder();
            foreach (string strfile in files)
            {
                sBuff.AppendFormat("{0},", strfile);
            }
            _locIniData[_curComponent.hash].AddKey("Unload", sBuff.ToString().TrimEnd(','));
            iniParser.WriteFile(componentnamePath, _locIniData);
        }


        private void Btn_LeftClick(object sender, EventArgs e)
        {
            if (UserINISettings.Instance.第一次下载扩展.Value)
            {
                XNAMessageBox.Show(WindowManager, "提示", "这里的扩展未能全部详细测试,若游玩过程遇到问题\n\n请不要联系原作者,\n\n先反馈重聚制作组,由我们详细测试复现后会反馈原作者,\n感谢大家理解和配合.");
                UserINISettings.Instance.第一次下载扩展.Value = false;
                UserINISettings.Instance.SaveSettings();
                return;
            }
            XNAClientButton button = (XNAClientButton)sender;
            if (button.Text == "安装")
            {
                string strMsg = string.Format("您确认安装 [{0}:{1}] 吗？文件大小：{2}", _curComponent.typeName, _curComponent.name, GetsizeString(_curComponent.size));
                var msgBox = XNAMessageBox.ShowYesNoDialog(WindowManager, "提示", strMsg);
                msgBox.NoClickedAction += MsgBox_InstallNoClicked;
                msgBox.YesClickedAction += MsgBox_InstallYesClicked;
            }
            else if (button.Text == "卸载")
            {
                UnInstall();
                RefreshInstallButtonStatus(CompList.SelectedIndex);
            }
            else
            {
                string strMsg = string.Format("您确认升级 [{0}:{1}] 吗？文件大小：{2}", _curComponent.typeName, _curComponent.name, GetsizeString(_curComponent.size));
                var msgBox = XNAMessageBox.ShowYesNoDialog(WindowManager, "提示", strMsg);
                msgBox.NoClickedAction += MsgBox_InstallNoClicked;
                msgBox.YesClickedAction += MsgBox_InstallYesClicked;
            }
        }

        private async void MsgBox_InstallYesClicked(XNAMessageBox messageBox)
        {
            CompList.Enabled = false;
            await DownloadfilesAsync();
            CompList.Enabled = true;
            需要刷新 = true;
        }

        private void MsgBox_InstallNoClicked(XNAMessageBox messageBox)
        {
            var parent = (DarkeningPanel)messageBox.Parent;
            parent.Hide();
        }

        private void UnInstall()
        {
            UserINISettings.Instance.取消渲染地图?.Invoke();
            if (null == _curComponent)
                return;
            
                var secData = _locIniData.Sections[_curComponent.hash];
                string strUnload = secData["Unload"].ToString();
                string[] lstDelfiles = strUnload.Split(',');
                
                foreach(string strfile in lstDelfiles)
                {
                try
                {
                    var filePath = Path.Combine(ProgramConstants.GamePath, strfile);
                    if (_curComponent.typeName == "地图" || _curComponent.typeName == "地图包")
                    {
                        filePath = Path.Combine(ProgramConstants.GamePath, "Maps/Multi/WorkShop", strfile);
                    }
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    continue;
                }

                _locIniData.Sections.RemoveSection(_curComponent.hash);
                iniParser.WriteFile(componentnamePath, _locIniData);
                需要刷新 = true;
                WindowManager.Report();
        }
            
           // UserINISettings.Instance.继续渲染地图?.Invoke();
        }

        public void InstallComponent(int reg_name)
        {
            var btn = installationButtons[reg_name];
            btn.AllowClick = false;
        }

        public void 预写地图配置(Component component)
        {
            var workshopIniPath = "./Maps/Multi/MPMapsWorkShop.ini";
            if(!File.Exists(workshopIniPath))
                File.Create(workshopIniPath).Close();
            var ini = new IniFile(workshopIniPath);
            var sectionName = $"Maps/Multi/WorkShop/{component.file}";
            if (!ini.SectionExists(sectionName))
                ini.AddSection(sectionName);
            ini.SetValue(sectionName, "Description", component.name)
                .SetValue(sectionName, "Author", component.author)
                .SetValue(sectionName, "Mission", sectionName);
            ini.WriteIniFile();
        }

        public override bool Save()
        {
            if (需要刷新)
                UserINISettings.Instance.ReLoadMissionList?.Invoke();
            需要刷新 = false;
            return false;
        }

        public void CancelAllDownloads()
        {
            Logger.Log("Cancelling all custom component downloads.");

            downloadCancelled = true;
        }

        public void Open()
        {
            downloadCancelled = false;
        }

        private string GetsizeString(long size)
        {
            if (size < 1048576)
            {
                return string.Format("{0:F2}KB", size * 1.0 / 1024);
            }
            else if (size >= 1048576 && size < 1073741824)
            {
                return string.Format("{0:F2}MB", size * 1.0 / 1048576);
            }
            else
                return string.Format("{0:F2}GB", size * 1.0 / 1073741824);
        }
    }
}
