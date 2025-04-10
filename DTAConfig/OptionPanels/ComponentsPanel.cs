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
using Localization;
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
using System.Threading;

namespace DTAConfig.OptionPanels
{
   public class ComponentsPanel : XNAOptionsPanel
    {
        private const string COMPONENTS_FILE = "该用户所有组件";

        private XNAMultiColumnListBox CompList;

        private XNALabel labeltypes;

        public XNAClientDropDown comboBoxtypes;

        private XNATextBox textBoxSearch;

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
                labeltypes.Text = "Filter".L10N("UI:DTAConfig:Filter");
                AddChild(labeltypes);

                comboBoxtypes = new XNAClientDropDown(WindowManager);
                comboBoxtypes.Name = nameof(comboBoxtypes);
                comboBoxtypes.ClientRectangle = new Rectangle(labeltypes.Right + 10, labeltypes.Top - 3, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
                
                comboBoxtypes.SelectedIndex = 0;
                comboBoxtypes.SelectedIndexChanged += ComboBoxtypes_SelectedIndexChanged;
                AddChild(comboBoxtypes);

                var lblSearch = new XNALabel(WindowManager)
                {
                    Text = "Search".L10N("UI:DTAConfig:Search"),
                    ClientRectangle = new Rectangle(comboBoxtypes.Right + 80, labeltypes.Y,0,0)
                };
                AddChild(lblSearch);

                textBoxSearch = new XNATextBox(WindowManager);
                textBoxSearch.Name = nameof(textBoxSearch);
                textBoxSearch.ClientRectangle = new Rectangle(lblSearch.Right + 50,comboBoxtypes.Y, 200, UIDesignConstants.BUTTON_HEIGHT);
                textBoxSearch.TextChanged += ComboBoxtypes_SelectedIndexChanged;
                AddChild(textBoxSearch);

                CompList = new XNAMultiColumnListBox(WindowManager);
                CompList.Name = nameof(CompList);
                CompList.ClientRectangle = new Rectangle(labeltypes.Left, labeltypes.Bottom + 10, Width - 40, Height - 75);
                CompList.SelectedIndexChanged += CompList_SelectedChanged;
                CompList.LineHeight = 30;
                CompList.FontIndex = 1;

                CompList.AddColumn("Serial number".L10N("UI:DTAConfig:Serialnumber"), 60)
                    .AddColumn("Component".L10N("UI:DTAConfig:Component"), CompList.Width - 420)
                    .AddColumn("Type".L10N("UI:DTAConfig:Type"), 110)
                    .AddColumn("Author".L10N("UI:DTAConfig:Author"), 90)
                    .AddColumn("Version".L10N("UI:DTAConfig:Version"), 70)
                    .AddColumn("Status".L10N("UI:DTAConfig:Status"), 90);
                AddChild(CompList);

                var _menu = new XNAContextMenu(WindowManager);
                _menu.Name = nameof(_menu);
                _menu.Width = 100;

                _menu.AddItem(new XNAContextMenuItem
                {
                    Text = "Refresh".L10N("UI:DTAConfig:Refresh"),
                    SelectAction = () => {
                        InitialComponets();
                    }
                });
                _menu.AddItem("Check out the introduction".L10N("UI:DTAConfig:Checkouttheintroduction"),查看介绍,null,判断是否有介绍);

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

                EnabledChanged += ComponentsPanel_EnabledChanged;
        }
        catch (Exception ex)
        {
            Logger.Log("组件初始化出错：" + ex);
        }
        }

        private void ComponentsPanel_EnabledChanged(object sender, EventArgs e)
        {
            if(Enabled)
                InitialComponets();
        }

        private bool 判断是否有介绍() => !string.IsNullOrEmpty(_curComponent?.description);
       

        private void 查看介绍()
        {
            XNAMessageBox.Show(WindowManager, _curComponent.name, _curComponent.description);
        }

        private void ComboBoxtypes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxtypes.SelectedIndex < 0 || comboBoxtypes.SelectedIndex >= comboBoxtypes.Items.Count)
                return;

            if (null == All_components || 0 == All_components.Count)
            {
                comboBoxtypes.SelectedIndex = 0;
                return;
            }
         
         //   if (0 < comboBoxtypes.SelectedIndex)
          //  {
                CompList.ClearItems();
                
                _components = All_components
                .FindAll(p => comboBoxtypes.SelectedIndex == 0 || p.type == comboBoxtypes.SelectedIndex - 1)
                .FindAll(p => p.name.Contains(textBoxSearch.Text.TrimEnd()))
                ;
              
          //  }
            InitialComponetsList(_components);
            CompList_SelectedChanged(null, null);
            CompList.TopIndex = -1;
        }


        private void InitialComponets()
        {
            comboBoxtypes.Items.Clear();
            comboBoxtypes.AddItem("All".L10N("UI:DTAConfig:All"));
            Task.Run(async () => {
                var Types = (await NetWorkINISettings.Get<string>("dict/getValue?section=component&key=type")).Item1?.Split(",") ?? [];
                foreach (var item in Types) comboBoxtypes.AddItem(item);
            });

            var context = SynchronizationContext.Current;
            Task.Run(async () =>
            {
#if RELEASE
                All_components = (await NetWorkINISettings.Get<List<Component>>("component/getAuditComponent")).Item1??[];
#else
                //All_components = (await NetWorkINISettings.Get<List<Component>>("component/getUnAuditComponent")).Item1??[];
                All_components = (await NetWorkINISettings.Get<List<Component>>("component/getAuditComponent")).Item1 ?? [];
#endif

                if (All_components.Count > 0)
                {
                    context?.Post(_ =>
                    {
                        ComboBoxtypes_SelectedIndexChanged(null, null);
                    }, null);
                }

            });
        }

        private void InitialComponetsList(List<Component> components)
        {
            if (null == components || null == CompList)
                return;
            CompList.ClearItems();
            int i = 1;
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
            StateItem state = new StateItem { Code = -1, Text = "Not available".L10N("UI:DTAConfig:Notavailable"), TextColor = Color.Red };
            string strid = comp.hash;
            if (string.IsNullOrEmpty(strid))
                return state;

            state = new StateItem { Code = 0, Text = "Not installed".L10N("UI:DTAConfig:Notinstalled"), TextColor = Color.Orange };
            var parser = new FileIniDataParser();
            foreach (SectionData locSec in _locIniData.Sections)
            {
                if (strid == locSec.SectionName)
                {
                    state = new StateItem { Code = 1, Text = "Installed".L10N("UI:DTAConfig:Installed"), TextColor = Color.Green };
                    if (CheckVersionNew(locSec.Keys["version"], comp.version))
                    {
                        state = new StateItem { Code = 2, Text = "Updatable".L10N("UI:DTAConfig:Updatable"), TextColor = Color.AliceBlue };
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
            if (CompList.SelectedIndex < 0 || CompList.SelectedIndex >= CompList.ItemCount)
                return;

            if (progressBar != null)
                progressBar.Visible = false;

            RefreshInstallButtonStatus(CompList.SelectedIndex);
        }

        private void RefreshInstallButtonStatus(int nIndex)
        {
            if (nIndex < 0 || nIndex >= _components.Count || !this.Visible || _components.Count == 0)
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
                    mainButton.Text = "Install".L10N("UI:DTAConfig:Install");
                else if (1 == item.Code)
                    mainButton.Text = "Uninstall".L10N("UI:DTAConfig:Uninstall");
                else
                    mainButton.Text = "Update".L10N("UI:DTAConfig:Update");
            }
            else
                mainButton.Visible = false;
        }

        private async Task DownloadfilesAsync()
        {
            if (null == _curComponent)
                return;

            
            mainButton.Visible = false;
            progressBar.Visible = true;
            lbstatus.Visible = true;
            lbprogress.Visible = true;
            

          
            string strLocPath = string.Empty;

            lbstatus.Text = "Downloading...".L10N("UI:DTAConfig:Downloading");

            try
            {
                using WebClient webClient = new WebClient();
                TaskbarProgress.Instance.SetState(TaskbarProgress.TaskbarStates.Normal);
                webClient.DownloadProgressChanged += (s, evt) =>
                {
                    progressBar.Value = evt.ProgressPercentage;

                    lbprogress.Text = progressBar.Value.ToString() + "%";

                    TaskbarProgress.Instance.SetValue(evt.ProgressPercentage,100);
                };
                
                var (strDownPath, message) = (await NetWorkINISettings.Get<string>($"component/getComponentUrl?id={_curComponent.id}"));

                if (string.IsNullOrEmpty(strDownPath))
                {
                    XNAMessageBox.Show(WindowManager, "Tips".L10N("UI:Main:Tips"), $"Failed to get the component package link:{message}".L10N("UI:DTAConfig:FailedGetComponentLink"));
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
                
                mainButton.Visible = true;
                progressBar.Visible = false;
                lbstatus.Visible = false;
                lbprogress.Visible = false;
                
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
                        XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), $"The file may be corrupted, please download it again".L10N("UI:DTAConfig:FileCorrupted"));
                        
                            mainButton.Visible = true;
                            progressBar.Visible = false;
                            lbstatus.Visible = false;
                            lbprogress.Visible = false;
                        
                        RefreshInstallButtonStatus(CompList.SelectedIndex);
                        return;
                    }
                }
                lbstatus.Text = "Unzipping...".L10N("UI:DTAConfig:Unzipping");
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

                    TaskbarProgress.Instance.SetState(TaskbarProgress.TaskbarStates.NoProgress);
                    WriteConponentConfig(SevenZip.GetFile(strLocPath));
                    try
                    {
                        File.Delete(strLocPath);
                    }catch (Exception ex)
                    {
                        Logger.Log(ex.ToString());
                    }
                });
                mainButton.Text = "Uninstall".L10N("UI:DTAConfig:Uninstall");
            }
            else
                mainButton.Text = "Install".L10N("UI:DTAConfig:Install");

            
                mainButton.Visible = true;
                progressBar.Visible = false;
                lbstatus.Visible = false;
                lbprogress.Visible = false;
            
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
                XNAMessageBox.Show(WindowManager, "Tips".L10N("UI:Main:Tips"), "If you encounter problems during the game, please do not contact the original author directly, and give priority to the Reunion production team, and we will give feedback to the original author after detailed testing and verification, thank you for your understanding and cooperation".L10N("UI:DTAConfig:FirstDownloadComponentTips"));
                UserINISettings.Instance.第一次下载扩展.Value = false;
                UserINISettings.Instance.SaveSettings();
                return;
            }
            XNAClientButton button = (XNAClientButton)sender;
            if (button.Text == "Install".L10N("UI:DTAConfig:Install"))
            {
                string strMsg = string.Format("Do you confirm the installation of [{0}:{1}]? File size: {2}".L10N("UI:DTAConfig:InstallComponentTips"), _curComponent.typeName, _curComponent.name, GetsizeString(_curComponent.size));
                var msgBox = XNAMessageBox.ShowYesNoDialog(WindowManager, "Tips".L10N("UI:Main:Tips"), strMsg);
                msgBox.NoClickedAction += MsgBox_InstallNoClicked;
                msgBox.YesClickedAction += MsgBox_InstallYesClicked;
            }
            else if (button.Text == "Uninstall".L10N("UI:DTAConfig:Uninstall"))
            {
                UnInstall();
                RefreshInstallButtonStatus(CompList.SelectedIndex);
            }
            else
            {
                string strMsg = string.Format("Do you confirm the upgrade [{0}:{1}]? File size: {2}".L10N("UI:DTAConfig:UpgradeComponentTips"), _curComponent.typeName, _curComponent.name, GetsizeString(_curComponent.size));
                var msgBox = XNAMessageBox.ShowYesNoDialog(WindowManager, "Tips".L10N("UI:Main:Tips"), strMsg);
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
            RenderImage.CancelRendering();
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
                    string pngFilePath = Path.ChangeExtension(filePath, ".png");
                    if (File.Exists(pngFilePath)) File.Delete(pngFilePath);
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
            
           // RenderImage.RenderImagesAsync();
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
                UserINISettings.Instance.重新加载地图和任务包?.Invoke();
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
