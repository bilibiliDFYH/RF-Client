using System;
using System.IO;
using System.Windows.Forms;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using ToolTip = ClientGUI.ToolTip;
using XNATextBox = ClientGUI.XNATextBox;
using Localization.Tools;
using System.Linq;
using DTAConfig.Entity;
using ClientCore;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Diagnostics;

namespace DTAConfig.OptionPanels;

public class ModManager : XNAWindow
{
    private static ModManager _instance;

    //public XNADropDown AI;

    public XNADropDown DDModAI;
    public XNAListBox ListBoxModAi;
    private XNAMultiColumnListBox _mcListBoxInfo;
    private ToolTip _tooltip;
    private XNAClientButton _btnReturn;
    public XNAClientButton BtnNew;
    public XNAClientButton BtnDel;

    public delegate void MyEventHandler(object sender, EventArgs e);
    private XNAContextMenu _modMenu;
    public event MyEventHandler MyEvent;


    public override void Initialize()
    {
        base.Initialize();
        
        ClientRectangle = new Rectangle(0, 0, 640, 384);
        CenterOnParent();

        var lblTittle = new XNALabel(WindowManager)
        {
            Text = "模组管理器",
            FontIndex = 0,
            ClientRectangle = new Rectangle(Width / 2 - 40, 5, 0, 0),
            TextColor = Color.White
        };

        AddChild(lblTittle);

        DDModAI = new XNADropDown(WindowManager)
        {
            ClientRectangle = new Rectangle(25, 30, 80, 40)
        };

        DDModAI.AddItem(["Mod","AI","任务包"]);
        
        AddChild(DDModAI);

        _modMenu = new XNAContextMenu(WindowManager);
        _modMenu.Name = nameof(_modMenu);
        _modMenu.Width = 100;

        _modMenu.AddItem(new XNAContextMenuItem
        {
            Text = "新增",
            SelectAction = () => BtnNew.OnLeftClick()
        });
        _modMenu.AddItem(new XNAContextMenuItem
        {
            Text = "修改",
            SelectAction = UpdateBase
        });
        _modMenu.AddItem(new XNAContextMenuItem
        {
            Text = "删除",
            SelectAction = () => BtnDel.OnLeftClick()
        });
        _modMenu.AddItem(new XNAContextMenuItem { Text = "编辑CSF", SelectAction = EditCsf });

        AddChild(_modMenu);

        ListBoxModAi = new XNAListBox(WindowManager)
        {
            ClientRectangle = new Rectangle(DDModAI.X, DDModAI.Y + 40, 130, 260),
            LineHeight = 25,
            SelectedIndex = 2,
            FontIndex = 2
        };
        ListBoxModAi.RightClick += (_, _) =>
        {
            ListBoxModAi.SelectedIndex = ListBoxModAi.HoveredIndex;

            if (ListBoxModAi.SelectedIndex == -1 || DDModAI.SelectedIndex == 1)
                return;
           
            var filePath = ((InfoBaseClass)ListBoxModAi.SelectedItem.Tag).FilePath;

            // 没有csf
            if (Directory.Exists(filePath) && Directory.GetFiles(filePath, "*.csf").Length == 0)
            {
                _modMenu.Items[3].Visible = false;
                
            }
            else
            {
                _modMenu.Items[3].Visible = true;
            }

            _modMenu.Open(GetCursorPoint());
        };
        AddChild(ListBoxModAi);

        _mcListBoxInfo = new XNAMultiColumnListBox(WindowManager)
        {
            ClientRectangle = new Rectangle(ListBoxModAi.X + ListBoxModAi.Width + 30, ListBoxModAi.Y, 420, ListBoxModAi.Height),
            LineHeight = 25,
            FontIndex = 2
        }.AddColumn("属性", 160).AddColumn("信息", 260) ;

        _mcListBoxInfo.SelectedIndexChanged += McListBoxInfoSelectedIndexChanged;
           
        AddChild(_mcListBoxInfo);

        DDModAI.SelectedIndexChanged += DDModAI_SelectedIndexChanged;

        _tooltip = new ToolTip(WindowManager, _mcListBoxInfo)
        {
            Text = "选择可查看详细信息"
        };
        


        _btnReturn = new XNAClientButton(WindowManager)
        {
            Text = "确定",
            ClientRectangle = new Rectangle(280, 340, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        _btnReturn.LeftClick += BtnReturn_LeftClick;
        AddChild(_btnReturn);

        BtnNew = new XNAClientButton(WindowManager)
        {
            Visible = false,
            ClientRectangle = new Rectangle(_mcListBoxInfo.X, DDModAI.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        BtnNew.LeftClick += BtnNew_LeftClick;
        AddChild(BtnNew);

        BtnDel = new XNAClientButton(WindowManager)
        {
            Visible = false,
            ClientRectangle = new Rectangle(BtnNew.X + 120, DDModAI.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        BtnDel.LeftClick += BtnDel_LeftClick;
        AddChild(BtnDel);

        var btnReload = new XNAClientButton(WindowManager)
        {
            Text = "刷新",
            ClientRectangle = new Rectangle(BtnDel.X + 120, DDModAI.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        btnReload.LeftClick += (_,_) => ReLoad();
        AddChild(btnReload);

        Enabled = false;
        ReLoad();
    }

    private void EditCsf()
    {
        var csfPath = Path.Combine(((InfoBaseClass)ListBoxModAi.SelectedItem.Tag).FilePath, "ra2md.csf");
        if (!File.Exists(csfPath))
            csfPath = Path.Combine(ProgramConstants.GamePath, "ra2md.csf");
        var csf = new CSF(csfPath);
        var editCSFWindows = new EditCSFWindows(WindowManager, _tooltip,csf);
        editCSFWindows.Show();

    }

    private void DDModAI_SelectedIndexChanged(object sender, EventArgs e)
    {

        //ListBoxModAi.SelectedIndexChanged -= ListBoxModAISelectedIndexChanged;
        //ListBoxModAi.SelectedIndex = -1;
        //ListBoxModAi.Clear();

        //IEnumerable<InfoBaseClass> infoBases = DDModAI.SelectedIndex switch
        //{
        //    0 => Mod.Mods,
        //    1 => AI.AIs,
        //    2 => MissionPack.MissionPacks,
        //    -1 => Mod.Mods,
        //};

        //foreach (var baseInfo in infoBases){
        //    ListBoxModAi.AddItem(new XNAListBoxItem { Text = baseInfo.Name, Tag = baseInfo });
        //}

        //ListBoxModAi.SelectedIndexChanged += ListBoxModAISelectedIndexChanged;
        //ListBoxModAi.SelectedIndex = 0;

        ListBoxModAi.SelectedIndexChanged -= ListBoxModAISelectedIndexChanged;
        ListBoxModAi.SelectedIndex = -1;
        ListBoxModAi.Clear();

        switch (DDModAI.SelectedIndex)
        {
            //Mod
            case 0:
                {
                    foreach (var mod in Mod.Mods)
                    {
                        ListBoxModAi.AddItem(new XNAListBoxItem { Text = mod.Name, Tag = mod });
                    }

                    break;
                }
            //AI
            case 1:
                {
                    foreach (var mod in AI.AIs)
                    {
                        ListBoxModAi.AddItem(new XNAListBoxItem { Text = mod.Name, Tag = mod });
                    }

                    break;
                }
            //任务包
            case 2:
                {
                    foreach (var missionPack in MissionPack.MissionPacks)
                    {
                        ListBoxModAi.AddItem(new XNAListBoxItem { Text = missionPack.Name, Tag = missionPack });
                    }

                    break;
                }
        }

        ListBoxModAi.SelectedIndexChanged += ListBoxModAISelectedIndexChanged;
        ListBoxModAi.SelectedIndex = 0;
    }

    /// <summary>
    /// 复制Mod文件
    /// </summary>
    /// <param name="modPath">mod原路径</param>
    /// <param name="mod">mod信息</param>
    private void CopyModFile(string modPath,Mod mod,bool covCsf)
    {
        #region 导入Mod文件

        //提取Mod文件
        if(!Directory.Exists(mod.FilePath))
            Directory.CreateDirectory(mod.FilePath);

        //提取mix文件
        HashSet<string> mixFileExclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Ares.mix", "ra2md.mix", "ra2.mix", "langmd.mix", "language.mix", "movmd03.mix", "multimd.mix",
            "movmd01.mix", "movmd02.mix","NPatch.mix","movies01.mix","movies02.mix","MAPS02.mix","MAPS01.mix",
            ProgramConstants.CORE_MIX,ProgramConstants.SKIN_MIX,ProgramConstants.MISSION_MIX
        };

        try
        {

            // 复制除了指定列表之外的所有.mix文件，同时处理以"Ecache"或"EXPAND"开头的文件
            int expandCounter = 2; // 统一的扩展文件计数器
            foreach (var mix in Directory.GetFiles(modPath, "*.mix"))
            {
                string fileName = Path.GetFileName(mix);
                bool isEcacheOrExpand = fileName.StartsWith("Ecache", StringComparison.OrdinalIgnoreCase) ||
                                        fileName.StartsWith("EXPAND", StringComparison.OrdinalIgnoreCase);

                // 判断是否在排除列表中，或者是否需要特殊处理
                if (!mixFileExclude.Contains(fileName) && !isEcacheOrExpand)
                {
                    File.Copy(mix, Path.Combine(mod.FilePath, fileName), true);
                }
                else if (isEcacheOrExpand)
                {
                    string newFileName;
                    do
                    {
                        newFileName = $"expandmd{expandCounter++.ToString("D2")}.mix";
                    }
                    while (File.Exists(Path.Combine(mod.FilePath, newFileName)));

                    File.Copy(mix, Path.Combine(mod.FilePath, newFileName), true);
                }
            }

            List<string> allFiles =
            [
                //提取shp文件
                .. Directory.GetFiles(modPath, "*.shp"),
            //提取pal文件
            .. Directory.GetFiles(modPath, "*.pal"),
            //提取vxl文件
            .. Directory.GetFiles(modPath, "*.vxl"),
        ];

            //提取dll文件，排除Ares.dll,Phobos.dll,cncnet5.dll,BINKW32.DLL,Blowfish.dll,qres32.dll,rename.dll
            string[] dllFiles = { "Ares.dll", "Phobos.dll", "cncnet5.dll", "BINKW32.DLL", "Blowfish.dll", "qres32.dll", "rename.dll", "wsock32.dll", "ddraw.dll" };
            foreach (var dll in Directory.GetFiles(modPath, "*.dll"))
            {
                if (!Array.Exists(dllFiles, file => file.Equals(Path.GetFileName(dll), StringComparison.OrdinalIgnoreCase)))
                    File.Copy(dll, Path.Combine(mod.FilePath, Path.GetFileName(dll)), true);
            }
            foreach (var bag in Directory.GetFiles(modPath, "*.bag"))
            {
                File.Copy(bag, Path.Combine(mod.FilePath, Path.GetFileName(bag)), true);
            }

            foreach (var idx in Directory.GetFiles(modPath, "*.idx"))
            {
                File.Copy(idx, Path.Combine(mod.FilePath, Path.GetFileName(idx)), true);
            }


            //提取ini文件，除了RA2(MD).ini
            foreach (var ini in Directory.GetFiles(modPath, "*.ini"))
            {
                if (!string.Equals(Path.GetFileName(ini), $"RA2{mod.md}.ini", StringComparison.OrdinalIgnoreCase) && !string.Equals(Path.GetFileName(ini), "ddraw.ini", StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(ini, Path.Combine(mod.FilePath, Path.GetFileName(ini)), true);
                    if (Path.GetFileName(ini) == "rules.ini")
                    {
                        var rules = new Rampastring.Tools.IniFile(Path.Combine(mod.FilePath, Path.GetFileName(ini)));
                        var mvc = rules.GetStringValue("General", "BaseUnit", "AMCV,SMCV");
                        if (mvc.Split(",").Length < 3)
                            rules.SetStringValue("General", "BaseUnit", mvc + ",null;尤复引擎玩原版必须要有三个基地车");
                        rules.WriteIniFile();
                        allFiles.Add(Path.Combine(mod.FilePath, Path.GetFileName(ini)));
                        mod.ExtensionOn = false;
                    }
                    else
                        allFiles.Add(ini);
                }
            }



            //提取CSF文件
            foreach (var csf in Directory.GetFiles(modPath, "*.csf"))
            {
                if (covCsf)
                {
                    var d = new CSF(csf).GetCsfDictionary();

                    if (d == null)
                    {
                        File.Copy(csf, Path.Combine(mod.FilePath, Path.GetFileName(csf)), true);
                    }
                    else
                    {
                        d.ConvertValuesToSimplified();
                        CSF.WriteCSF(d, Path.Combine(mod.FilePath, Path.GetFileName(csf).Equals("ra2.csf", StringComparison.OrdinalIgnoreCase) ? "ra2md.csf" : Path.GetFileName(csf)));
                    }

                   
                }
                else
                    File.Copy(csf, Path.Combine(mod.FilePath, Path.GetFileName(csf).Equals("ra2.csf", StringComparison.OrdinalIgnoreCase) ? "ra2md.csf" : Path.GetFileName(csf)), true);

            }

            //提取font
            foreach (var fnt in Directory.GetFiles(modPath, "*.fnt"))
            {
                allFiles.Add(fnt);
            }

            Mix.PackFilesToMix(allFiles, mod.FilePath, ProgramConstants.MOD_MIX);
        }
        catch(Exception ex)
        {
            XNAMessageBox.Show(WindowManager, "错误", $"文件操作失败，原因：{ex}");
        }
       
        mod.Create(); //写入INI文件

       
        ReLoad(); //重新载入
        #endregion
    }

    /// <summary>
    /// 导入Mod
    /// </summary>
    private void ImportMod()
    {
        //让玩家选择mod文件夹
        var folderBrowser = new FolderBrowserDialog();
        folderBrowser.Description = "请选择Mod所在的目录";
        //folderBrowser.ShowNewFolderButton = true;
        if (DialogResult.OK != folderBrowser.ShowDialog())
            return;

        var modPath = folderBrowser.SelectedPath;
        var DirectoryName = Path.GetFileName(modPath);

        if (DirectoryName.IndexOf("RA2MD") != -1)
        {
            var warnBox = new XNAMessageBox(WindowManager, "错误", "路径中不能包含 RA2MD ", XNAMessageBoxButtons.OK);
            warnBox.Show();
            return;
        }

        var submitbox = new XNAMessageBox(WindowManager, "错误",
            $"Mod路径可能不正确(不存在gamemd.exe或game.exe)，" +
            $"{Environment.NewLine}你确定要导入吗？",XNAMessageBoxButtons.YesNo);

        var mod = new Mod();
        submitbox.YesClickedAction += (_) => {
            #region Mod初步判断
            var aresVerison = string.Empty;
            string extensionPath = "Mod&AI\\Extension";
            //检测ARES
            if (File.Exists(Path.Combine(modPath, "Ares.dll")))
            {
                //aresVerison = System.Diagnostics.FileVersionInfo.GetVersionInfo(Path.Combine(modPath, "Ares.dll")).FileVersion;
                aresVerison = FileVersionInfo.GetVersionInfo(Path.Combine(modPath, "Ares.dll")).ProductVersion;

                mod.ExtensionOn = true;
                //如果用的自带的3.0p1
                if (aresVerison != "3.0p1")
                {
                    mod.Extension += $"Ares{aresVerison},";
                    if(Directory.Exists($"{extensionPath}\\Ares{aresVerison}"))
                        Directory.CreateDirectory($"{extensionPath}\\Ares\\Ares{aresVerison}");
                    File.Copy(Path.Combine(modPath, "Ares.dll"), $"{extensionPath}\\Ares\\Ares{aresVerison}\\Ares.dll"));
                }
                else
                {
                    mod.Extension += $"Ares3,";
                }
            }

            var phobosVersion = string.Empty;
            //检测Phobos
            if (File.Exists(Path.Combine(modPath, "Phobos.dll")))
            {
                phobosVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(Path.Combine(modPath, "Phobos.dll")).FileVersion;

                mod.ExtensionOn = true;
                //如果用的自带的36
                if (phobosVersion != "0.0.0.36")
                {
                    mod.Extension += $"Phobos{phobosVersion}";
                    if (Directory.Exists($"{extensionPath}\\Phobos{phobosVersion}"))
                        Directory.CreateDirectory($"{extensionPath}\\Phobos\\Phobos{phobosVersion}");
                    File.Copy(Path.Combine(modPath, "phobos.dll"), $"{extensionPath}\\Phobos\\phobos{phobosVersion}\\Phobos.dll"));
                }
                else
                {
                    mod.Extension += $"Phobos36";
                }
            }
            //检测NP

            //string newFilePath = Path.Combine(modPath, "NPatch.mix");

            //string CalculateMD5(string filePath)
            //{
            //    using (var md5 = MD5.Create())
            //    {
            //        using (var stream = File.OpenRead(filePath))
            //        {
            //            return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            //        }
            //    }
            //}

            //if (File.Exists(newFilePath))
            //{
                
            //    string extensionPath = "Mod&AI\\Extension";
               
            //    string modExtension = "";

            //    // 计算新文件的哈希值
            //    string newFileHash = CalculateMD5(newFilePath);

            //    // 检查Mod&AI\Extension目录下所有以NP开头的文件夹
            //    bool hashMatchFound = false;
            //    foreach (var dir in Directory.GetDirectories(extensionPath, "NP*"))
            //    {
            //        string nPathMixFile = Path.Combine(dir, "NPatch.mix");
            //        if (File.Exists(nPathMixFile))
            //        {
            //            // 计算NPath.mix文件的哈希值
            //            string nPathMixHash = CalculateMD5(nPathMixFile);

            //            // 比较哈希值
            //            if (nPathMixHash == newFileHash)
            //            {
            //                mod.Extension = new DirectoryInfo(dir).Name;
            //                hashMatchFound = true;
            //                break;
            //            }
            //        }
            //    }
            //    // 如果没有找到匹配的哈希值，创建新的NP文件夹
            //    if (!hashMatchFound)
            //    {
            //        string newDirName = "NP" + DateTime.Now.ToString("yyyyMMddHHmmss");
            //        string newDirPath = Path.Combine(extensionPath, newDirName);
            //        Directory.CreateDirectory(newDirPath);

            //        string newNPathMix = Path.Combine(newDirPath, "NPatch.mix");
            //        File.Copy(newFilePath, newNPathMix);

            //        mod.Extension = newDirName;
            //    }

            //}

            if (mod.Extension + "" == "")
            {
                mod.Extension = "Ares3,Phobos";
            }

            

            mod.ID = mod.Name = DirectoryName;
            

            mod.md = string.Empty;
            foreach (var file in new string[] { "rulesmd.ini", "artmd.ini", "expandmd01.mix", "RA2MD.ini", "ra2md.csf" })
            {
                if (File.Exists($"{modPath}/{file}"))
                {
                    mod.md = "md";
                }
            }
            mod.ExtensionOn = true;
            if (File.Exists(Path.Combine(modPath, $"rules{mod.md}.ini"))){
                mod.ExtensionOn = false;
                if (File.Exists(Path.Combine(modPath, $"ra2{mod.md}.csf")))
                {
                    var csf = new CSF(Path.Combine(modPath, $"ra2{mod.md}.csf")).GetCsfDictionary();
                    csf.ConvertValuesToSimplified();
                    if (csf != null)
                    {
                        var rules = new IniFile(Path.Combine(modPath, $"rules{mod.md}.ini"));
                        var countries = rules.GetSectionKeys("Countries");
                        mod.Countries = string.Empty;
                        foreach (var c in countries)
                        {

                            var country = rules.GetValue("Countries", c, string.Empty);
                            if (country == "GDI") break;
                            if (csf.ContainsKey($"Name:{country}"))
                                mod.Countries += csf[$"Name:{country}"] + ",";
                            else
                            {
                                mod.Countries = mod.md == string.Empty ? "美国,韩国,法国,德国,英国,利比亚,伊拉克,古巴,苏联," : "美国,韩国,法国,德国,英国,利比亚,伊拉克,古巴,苏联,尤里,";
                                break;
                            }

                        }

                        if (!string.IsNullOrEmpty(mod.Countries))
                        {
                            mod.Countries = mod.Countries.Remove(mod.Countries.Length - 1);
                        }
                    }
                }
            }

            mod.UseAI = mod.md == "md" ? "YRAI" : "RA2AI";

            #endregion

            var infoWindows = new ModInfoWindows(WindowManager, mod, "导入Mod");
            AddChild(infoWindows);

            // infoWindows.Enable();

            // 展示新增Mod界面
            infoWindows.EnabledChanged += (_, _) =>
            {
                mod = infoWindows.GetModInfo();
                bool csf = infoWindows.GetCsf();
                infoWindows.Dispose();
                if (mod == null)
                    return;
                CopyModFile(modPath, mod,csf);
            };
        };

        //判断路径是否合法
        if (File.Exists(Path.Combine(modPath, "gamemd.exe"))) 
        {
            //是尤复Mod
            mod.md = "md";
            mod.UseAI = "YRAI";
            submitbox.YesClickedAction.Invoke(submitbox);
        }
        else if (File.Exists(Path.Combine(modPath, "game.exe")))
        {
            //是原版Mod
            mod.md = string.Empty;
            mod.UseAI = "RA2AI";
            submitbox.YesClickedAction.Invoke(submitbox);
        }
        else
        {
            submitbox.Show();
        }
    }

    /// <summary>
    /// 导入任务包
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="report">是否显示警告</param>
    public void ImportMissionPack(string path = "",bool report = true)
    {
        if (string.IsNullOrEmpty(path))
        {
            //让玩家选择任务包文件夹
            var folderBrowser = new FolderBrowserDialog();
            folderBrowser.Description = "请选择任务包所在的目录";
            if (folderBrowser.ShowDialog() != DialogResult.OK)
                return;
            path = folderBrowser.SelectedPath;
        }
        #region 任务包初步判断
        var missionPackPath = path;

        var DirectoryName = Path.GetFileName(missionPackPath);

        var aresVerison = string.Empty;
        var mixs = Directory.GetFiles(missionPackPath, "*.mix");
     //   var maps = Directory.GetFiles(missionPackPath, "*.map");

        

        var csfs = Directory.GetFiles(missionPackPath, "*.csf");
        
        var csfExist = false;
        if (csfs.Length > 0)
        {
            csfExist = true;
        }
        //var resultBox = new XNAMessageBox(WindowManager, "提示", "此任务包中没有显著的地图文件，可能封装在mix里，是否尝试导入?", XNAMessageBoxButtons.YesNo);
        //resultBox.YesClickedAction += (_) =>
        //{
            var shps = Directory.GetFiles(missionPackPath, "*.shp")
            .Where(file => !Path.GetFileName(file).StartsWith("ls")&& !Path.GetFileName(file).StartsWith("gls"))
            .ToArray();
            var vxls = Directory.GetFiles(missionPackPath, "*.vxl");
            var pals = Directory.GetFiles(missionPackPath, "*.pal")
                .Where(file => !Path.GetFileName(file).StartsWith("ls") && !Path.GetFileName(file).StartsWith("gls"))
                .ToArray();

            var md = string.Empty;
            var missionPack = new MissionPack();
            missionPack.ID = missionPack.Name = DirectoryName;
            missionPack.BuildOffAlly = true;
            missionPack.Other = true;
            var modID = string.Empty;

            //如果有md.mix结尾的文件肯定是尤复任务
            if (Directory.GetFiles(missionPackPath, "*md.*").Length != 0)
            {
                md = "md";
                missionPack.YR = true;
            }
            else
            {
                missionPack.YR = false;
            }

            var allMaps = Directory.GetFiles(missionPackPath, "all*md.map");
            var sovMaps = Directory.GetFiles(missionPackPath, "sov*md.map");


            if (allMaps.Length + sovMaps.Length != 0)
            {
                md = "md";
            }
            else
            {
                allMaps = Directory.GetFiles(missionPackPath, "all*.map");
                sovMaps = Directory.GetFiles(missionPackPath, "sov*.map");
            }

            var isMod = (shps.Length + vxls.Length + pals.Length != 0 ||
                       File.Exists(Path.Combine(missionPackPath, $"rules{md}.ini")) ||
                       File.Exists(Path.Combine(missionPackPath, $"art{md}.ini")));

            //  Console.WriteLine(Path.Combine(missionPackPath, $"rules{md}"));

            missionPack.FilePath = $"Maps/Cp/{missionPack.ID}";
            missionPack.FileName = $"Maps/Cp/battle{missionPack.ID}.ini";

            //检测ARES
            if (File.Exists(Path.Combine(missionPackPath, "Ares.dll")))
            {
                aresVerison = System.Diagnostics.FileVersionInfo.GetVersionInfo(Path.Combine(missionPackPath, "Ares.dll")).FileVersion;

                //如果用的自带的3.0p1
                if (aresVerison != "3.0p1")
                    missionPack.Extension += $"Ares{aresVerison}";
            }

            var phobosVersion = string.Empty;

            //检测Phobos
            if (File.Exists(Path.Combine(missionPackPath, "Phobos.dll")))
            {
                phobosVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(Path.Combine(missionPackPath, "Phobos.dll")).FileVersion;

                //如果用的自带的36
                if (phobosVersion != "0.0.0.36")
                    missionPack.Extension += $"Phobos{phobosVersion}";
            }
            //检测NP
            //NP修改了Gamemd,所以对比哈希值，如果和原有的Gamemd对不上，那就当NP

            if (missionPack.Extension + "" == "")
            {
                missionPack.Extension = "Ares3,Phobos";
            }

            missionPack.Mod = isMod ? missionPack.ID : md == "md" ? "YR" : "RA2";

        var maps = 从mapselmd导入关卡($"{missionPackPath}/mapsel{md}.ini").ToArray();
        if (maps.Length == 0)
            maps = Directory.GetFiles(missionPackPath, "*.map");

        #endregion
        var infoWindows = new MissionPackInfoWindows(WindowManager, missionPack, "导入任务包", maps.Length == 0, csfExist);
            AddChild(infoWindows);

            // infoWindows.Enable();

            // 展示新增任务包界面
            infoWindows.EnabledChanged += async (_, _) =>
            {
                missionPack = infoWindows.GetMissionPackInfo();
                bool rander = infoWindows.GetRander();
                bool covCsf = infoWindows.GetCsf();
                int missionCount = infoWindows.missionCount;

                infoWindows.Dispose();

                if (missionPack == null)
                    return;

                //说明改动较大，先进行mod导入

                var mod = new Mod();

                var maps = Directory.GetFiles(missionPackPath, "*.map");

                if (isMod)
                {

                    mod.ID = DirectoryName;
                    mod.Name = missionPack.Name;
                    mod.md = md;
                    mod.UseAI = md == "md" ? "YRAI" : "RA2AI";

                    mod.FilePath = $"Mod&AI/Mod/{DirectoryName}";
                    mod.Extension = "Ares3,Phobos";
                    mod.MuVisible = false;
                    mod.Author = missionPack.Author;

                    //导入Mod
                    CopyModFile(missionPackPath, mod, covCsf);

                    if (File.Exists($"{mod.FilePath}/mapsel{md}.ini"))
                    {
                        maps = 从mapselmd导入关卡($"{mod.FilePath}/mapsel{md}.ini").ToArray();
                    }

                }

                #region 处理任务包文件和写入ini

                //提取任务包文件
                Directory.CreateDirectory(missionPack.FilePath);

            
                //提取map文件
                foreach (var map in maps)
                {
                //    var mapName = Path.Combine(missionPack.FilePath, Path.GetFileName(map));
                    if(File.Exists(map))
                        File.Copy(map, Path.Combine(missionPack.FilePath, Path.GetFileName(map)), true);
                }
                //Mix.PackFilesToMix(maps.ToList(), missionPack.FilePath, $"{ProgramConstants.MISSION_MIX}");

                if (!isMod)
                {
                    //提取CSF文件 如果是ra2.csf需要改为ra2md.csf
                    foreach (var csf in Directory.GetFiles(missionPackPath, "*.csf"))
                    {
                        if (covCsf)
                        {
                            var d = new CSF(csf).GetCsfDictionary();
                            if (d != null)
                            {
                                d.ConvertValuesToSimplified();
                                CSF.WriteCSF(d, Path.Combine(missionPack.FilePath, Path.GetFileName(csf).ToLower() == "ra2.csf" ? "ra2md.csf" : Path.GetFileName(csf)));
                                if (Path.GetFileName(csf).ToLower() == "ra2.csf" || Path.GetFileName(csf).ToLower() == "ra2md.csf")
                                    CSF.WriteCSF(d, Path.Combine(missionPack.FilePath, Path.GetFileName(csf).ToLower() == "ra2.csf" ? "stringtable00.csf" : Path.GetFileName(csf)));
                            }
                            else
                            {
                                File.Copy(csf, Path.Combine(missionPack.FilePath, Path.GetFileName(csf).ToLower() == "ra2.csf" ? "ra2md.csf" : Path.GetFileName(csf)), true);
                                if (Path.GetFileName(csf).ToLower() == "ra2.csf" || Path.GetFileName(csf).ToLower() == "ra2md.csf")
                                    File.Copy(csf, Path.Combine(missionPack.FilePath, Path.GetFileName(csf).ToLower() == "ra2.csf" ? "stringtable00.csf" : Path.GetFileName(csf)), true);
                            }
                        }
                        else
                        {
                            File.Copy(csf, Path.Combine(missionPack.FilePath, Path.GetFileName(csf).ToLower() == "ra2.csf" ? "ra2md.csf" : Path.GetFileName(csf)), true);
                            if (Path.GetFileName(csf).ToLower() == "ra2.csf" || Path.GetFileName(csf).ToLower() == "ra2md.csf")
                                File.Copy(csf, Path.Combine(missionPack.FilePath, Path.GetFileName(csf).ToLower() == "ra2.csf" ? "stringtable00.csf" : Path.GetFileName(csf)), true);
                        }
                    }
                }

                //提取font
                foreach (var fnt in Directory.GetFiles(missionPackPath, "*.fnt"))
                {
                    File.Copy(fnt, Path.Combine(missionPack.FilePath, Path.GetFileName(fnt)), true);
                }

                missionPack.Other = true;

                missionPack.Create(); //任务包写入INI文件

                var csfPath = Path.Combine(missionPack.FilePath, "ra2md.csf");
                if (isMod)
                    csfPath = Path.Combine(mod.FilePath, "ra2md.csf");

                var csfJson = new CSF(csfPath).GetCsfDictionary();

                var goodCsf = csfJson != null;

                var iniFile = new IniFile(missionPack.FileName);

                // Console.WriteLine(missionPack.FileName);

                var mission = maps;
                //var mission = allMaps.ToList();
                //mission.AddRange(sovMaps);

                var count = 0;
                if (!iniFile.SectionExists("Battles"))
                    iniFile.AddSection("Battles");
                iniFile.SetValue("Battles", missionPack.ID, missionPack.ID);
                foreach (var file in mission)
                {

                    var country = Path.GetFileName(file).ToUpper()[..3];
                    count++;

                    var key = $"{missionPack.ID + count}";

                    iniFile.SetValue("Battles", key, key);
                    if (!iniFile.SectionExists(key))
                        iniFile.AddSection(key);

                    iniFile.SetValue(key, "Scenario", Path.GetFileName(file).ToUpper());

                    //Console.WriteLine($"LoadMsg:{country}{Path.GetFileName(file).Substring(3, 2)}{(md != string.Empty ? "MD" : "")}");
                    if (csfJson != null)
                    {
                        // 构建我们要查找的键
                        string keyToSearch = $"LoadMsg:{country}{Path.GetFileName(file).Substring(3, 2)}{(md != string.Empty ? "MD" : "")}";

                        // 尝试从csfJson中获取值
                        if (csfJson.TryGetValue(keyToSearch, out var value))
                        {
                            // 如果找到了键，就替换换行符并设置值
                            iniFile.SetValue(key, "Description", value.Replace("\n", "@"));
                        }
                        else
                        {
                            // 如果在csfJson中找不到键，设置为“第{count}关”
                            iniFile.SetValue(key, "Description", $"第{count}关");
                        }
                    }
                    else
                    {
                        // 如果csfJson为null，也设置为“第{count}关”
                        iniFile.SetValue(key, "Description", $"第{count}关");
                    }

                    iniFile.SetValue(key, "MissionPack", missionPack.ID);

                    //判断国家
                    switch (country)
                    {
                        case "ALL":
                            iniFile.SetValue(key, "SideName", "Allied");
                            break;
                        case "SOV":
                            iniFile.SetValue(key, "SideName", "Soviet");
                            break;
                    }

                    if (goodCsf && (country == "ALL" || country == "SOV"))
                    {
                        // 构建我们要查找的键
                        string keyToSearch = $"BRIEF:{country}{Path.GetFileName(file).Substring(3, 2)}{(md != string.Empty ? "MD" : "")}";

                        // 尝试从 csfJson 中获取值
                        if (csfJson.TryGetValue(keyToSearch, out var value))
                        {
                            // 如果找到了键，就替换换行符并设置值
                            iniFile.SetValue(key, "LongDescription", value.Replace("\n", "@"));
                        }
                    }
                }
                iniFile.WriteIniFile();
                #endregion

                if (isMod)
                {

                    FileInfo spawnerSettingsFile = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);

                    IniFile spawnReader = new IniFile(spawnerSettingsFile.FullName);

                    string oldGame = spawnReader.GetValue("Settings", "Game", string.Empty);
                    string newGame = mod.FilePath;

                    //如果和前一次使用的游戏不一样
                    if (oldGame != newGame)
                    {
                        try
                        {
                            FileHelper.DelFiles(GetDeleteFile(oldGame));
                        }
                        catch
                        {
                            if (File.Exists($"{ProgramConstants.MOD_MIX}"))
                                File.Delete($"{ProgramConstants.MOD_MIX}");
                        }
                        FileHelper.CopyDirectory(newGame, "./");
                    }

                    spawnReader.SetValue("Settings", "Game", newGame);
                    spawnReader.WriteIniFile();
                }
                if (rander)
                    await RenderPreviewImageAsync(missionPack);
                ReLoad();
            };
     //   };
       // resultBox.NoClickedAction += (_) => { return; };

        ////判断是否有地图
        //if (maps.Length == 0)
        //{
        //    if (report) { 
        //        resultBox.Show();
        //    }

        //}
        //else
        //{
        //    missionMix = false;
        //    resultBox.YesClickedAction.Invoke(resultBox);
        //}

        
    }

    private List<string> 从mapselmd导入关卡(string mapselmd)
    {
        List<string> 关卡数 = [];
        if(string.IsNullOrEmpty(mapselmd)||!File.Exists(mapselmd)) return 关卡数;
        var iniFile = new IniFile(mapselmd);
        if (!iniFile.SectionExists("GDI")) return 关卡数;
        var sections = iniFile.GetSectionValues("GDI");
        从Section导入(sections);
        sections = iniFile.GetSectionValues("Nod");
        从Section导入(sections);

        void 从Section导入(List<string> sections)
        {
            foreach (var section in sections)
            {
                if (!iniFile.SectionExists(section)) continue;
                var map = iniFile.GetValue(section, "Scenario", string.Empty);
                if (map == string.Empty) continue;
                关卡数.Add(map);
            }
        }

        return 关卡数;
    }


    protected List<string> GetDeleteFile(string oldGame)
    {
        if (oldGame == null || oldGame == "")
            return null;

        List<string> deleteFile = new List<string>();

        foreach (string file in Directory.GetFiles(oldGame))
        {
            deleteFile.Add(Path.GetFileName(file));

        }

        return deleteFile;
    }


    private void UpdateBase()
    {
        if (DDModAI.SelectedIndex == 0)
            UpdateMod(ListBoxModAi.SelectedItem.Tag as Mod);
        if (DDModAI.SelectedIndex == 1)
            UpdateAI(ListBoxModAi.SelectedItem.Tag as AI);
        if (DDModAI.SelectedIndex == 2)
            UpdateMissionPack(ListBoxModAi.SelectedItem.Tag as MissionPack);
    }

    /// <summary>
    /// 修改Mod
    /// </summary>
    /// <param name="mod"></param>
    private void UpdateMod(Mod mod)
    {
        var infoWindows = new ModInfoWindows(WindowManager, mod, "修改Mod");

        var dp = DarkeningPanel.AddAndInitializeWithControl(WindowManager, infoWindows);

        infoWindows.EnabledChanged += (_, _) =>
        {
            mod = infoWindows.GetModInfo();
            var csf = infoWindows.GetCsf();
            infoWindows.Dispose();
            if (mod == null)
                return;

            mod.Create(); //写入INI文件

            if (csf)
            {
                foreach (var csfFile in Directory.GetFiles(mod.FilePath, "*.csf"))
                {

                    var d = new CSF(csfFile).GetCsfDictionary();

                    if (d == null)
                    {
                        File.Copy(csfFile, Path.Combine(mod.FilePath, Path.GetFileName(csfFile)), true);
                    }
                    else
                    {
                        d.ConvertValuesToSimplified();
                        CSF.WriteCSF(d, Path.Combine(mod.FilePath, Path.GetFileName(csfFile).Equals("ra2.csf", StringComparison.OrdinalIgnoreCase) ? "ra2md.csf" : Path.GetFileName(csfFile)));
                    }
                }
            }

            ReLoad(); //重新载入
        };
    }

    /// <summary>
    /// 修改AI
    /// </summary>
    /// <param name="ai"></param>
    private void UpdateAI(AI ai)
    {
        var infoWindows = new AIInfoWindows(WindowManager, ai, "修改AI");
        var dp = DarkeningPanel.AddAndInitializeWithControl(WindowManager, infoWindows);

        infoWindows.EnabledChanged += (_, _) =>
        {
            ai = infoWindows.GetAIInfo();
            infoWindows.Dispose();
            if (ai == null)
                return;

            ai.Create(); //写入INI文件

            ReLoad(); //重新载入
        };
    }

    /// <summary>
    /// 修改任务包
    /// </summary>
    /// <param name="pack"></param>
    private void UpdateMissionPack(MissionPack pack)
    {
        var missionMix = false;
        var csfExist = false;

        if(!Directory.Exists(pack.FilePath))
        {
            XNAMessageBox.Show(WindowManager, "信息", $"任务包路径{pack.FilePath}不存在！");
            return;
        }

        if (Directory.GetFiles(pack.FilePath, "*.map").Length == 0)
        {
            missionMix = true;
        }

        if (Directory.GetFiles(pack.FilePath, "*.csf").Length != 0)
        {
            csfExist = true;
        }

        var infoWindows = new MissionPackInfoWindows(WindowManager, pack, "修改任务包", missionMix, csfExist);
        var dp = DarkeningPanel.AddAndInitializeWithControl(WindowManager, infoWindows);

        //AddChild(infoWindows);

        infoWindows.EnabledChanged += async (_, _) =>
        {
            var missionPack = infoWindows.GetMissionPackInfo();
            bool rander = infoWindows.GetRander();
            bool csf = infoWindows.GetCsf();
            infoWindows.Dispose();
            if (missionPack == null)
                return;

            missionPack.Create(); //写入配置

            if(rander)
               await RenderPreviewImageAsync(missionPack);

            ReLoad(); //重新载入
        };
    }

    private async Task RenderPreviewImageAsync(MissionPack missionPack)
    {
        string[] mapFiles = Directory.GetFiles(missionPack.FilePath, "*.map");
        if(mapFiles.Length == 0) return;

        var messageBox = new XNAMessage(WindowManager);

        messageBox.caption = "渲染中...";
        messageBox.description = $"已渲染图像 0 / {mapFiles.Length}";
        messageBox.Show();

 
        void RenderCompletedHandler(object sender, EventArgs e)
        {
            
            messageBox.description = $"已渲染图像 {RenderImage.RenderCount} / {mapFiles.Length}";

            if (RenderImage.RenderCount == mapFiles.Length)
            {
                messageBox.Disable();
                // 渲染完成后，解除事件绑定
                RenderImage.RenderCompleted -= RenderCompletedHandler;
            }
        }

        RenderImage.RenderCompleted += RenderCompletedHandler;
 
         RenderImage.RenderImagesAsync(mapFiles);

    }

    private void BtnNew_LeftClick(object sender, EventArgs e)
    {
        if (DDModAI.SelectedIndex == 0)
            ImportMod();
        if (DDModAI.SelectedIndex == 2)
            ImportMissionPack();
    }


    private void ReLoad()
    {
        Mod.Load();
       
        MissionPack.Load();

        AI.Load();

       // listBoxModAI.Clear();

        DDModAI_SelectedIndexChanged(DDModAI,null);


        LoadModInfo();
    }


    private void BtnDel_LeftClick(object sender, EventArgs e)
    {
        if (!((InfoBaseClass)ListBoxModAi.SelectedItem.Tag).CanDel)
        {
            XNAMessageBox.Show(WindowManager,"错误","系统自带的无法被删除");
            return;
        }

        if (DDModAI.SelectedIndex == 2)
        {
            var missionPack = ListBoxModAi.SelectedItem.Tag as MissionPack;

            var inifile = new IniFile(missionPack.FileName);
            var m = string.Empty;

            foreach (var s in inifile.GetSections())
            {
                if(inifile.GetValue(s,"MissionPack",string.Empty) == missionPack.ID)
                 m += inifile.GetValue(s, "Description", s) + Environment.NewLine; 
            }

            var xNAMessageBox = new XNAMessageBox(WindowManager, "删除确认",
                $"您真的要删除任务包{missionPack.Name}吗？它包含以下任务：{Environment.NewLine}{m} ", XNAMessageBoxButtons.YesNo);
            xNAMessageBox.YesClickedAction += DelMissionPack;
            xNAMessageBox.NoClickedAction += (_) => ReLoad();
            xNAMessageBox.Show();
        }
        else
        {
            Mod mod = ListBoxModAi.SelectedItem.Tag as Mod;
            if (mod == null) return;
            foreach (var missionPack in MissionPack.MissionPacks)
            {
                if (missionPack.Mod.Contains(mod.ID))
                {
                    XNAMessageBox.Show(WindowManager, "错误", $"这个Mod被任务包 {missionPack.Name} 使用，无法删除，如要删除请删除任务包。");
                    return;
                }
            }

            XNAMessageBox xNAMessageBox = new XNAMessageBox(WindowManager, "删除确认",
                "您真的要删除Mod" + ListBoxModAi.SelectedItem.Text + "吗？", XNAMessageBoxButtons.YesNo);
            xNAMessageBox.YesClickedAction += DelMod;
            xNAMessageBox.NoClickedAction += (_) => ReLoad();
            xNAMessageBox.Show();
        }
    }

    private void DelMissionPack(XNAMessageBox box)
    {
        var missionPack = ListBoxModAi.SelectedItem.Tag as MissionPack;
        var iniFile = new IniFile(missionPack!.FileName);
        var messageBox = new XNAMessageBox(WindowManager, "删除确认", "需要同时删除本地文件吗", XNAMessageBoxButtons.YesNo);

        iniFile.SetValue("MissionPack", missionPack.ID, string.Empty);
        var modid = iniFile.GetValue(missionPack.ID, "Mod", string.Empty);
        if (!string.IsNullOrEmpty(modid)&&missionPack.ID == modid )
        {
            var mod = Mod.Mods.Find(m => m.ID == iniFile.GetValue(missionPack.ID, "Mod", string.Empty));
            if (mod != null)
            {
                var modiniFile = new IniFile(mod.FileName);
                modiniFile.SetValue("Mod", mod.ID, string.Empty);
                modiniFile.RemoveSection(mod.ID);
                modiniFile.WriteIniFile();
                messageBox.Tag = mod;
            }
        }
            foreach (var fore in iniFile.GetSections())
        {
            if (iniFile.GetValue(fore, "MissionPack", string.Empty) == missionPack.ID)
            {
                iniFile.SetValue("Battles", fore, string.Empty);
                iniFile.RemoveSection(fore);
            }

        }

        iniFile.WriteIniFile();
        
        messageBox.YesClickedAction += DelMissionPackFile;
        messageBox.NoClickedAction += (_) => ReLoad();
        messageBox.Show();
    }

    private void DelMod(XNAMessageBox box)
    {
       
        var mod = ListBoxModAi.SelectedItem.Tag as Mod;
        var iniFile = new IniFile(mod!.FileName);
        iniFile.SetValue("Mod",mod.ID,string.Empty);
        iniFile.RemoveSection(mod.ID);
        iniFile.WriteIniFile();
        var messageBox = new XNAMessageBox(WindowManager, "删除确认", "需要同时删除本地Mod文件吗",XNAMessageBoxButtons.YesNo);
        
        messageBox.YesClickedAction += DelModFile;
        messageBox.NoClickedAction += (_) => ReLoad();
        messageBox.Show();
        
    }

    private void DelModFile(XNAMessageBox messageBox)
    {
        try
        {
            Directory.Delete(((Mod)ListBoxModAi.SelectedItem.Tag).FilePath, true);
        }
        catch
        {
            XNAMessageBox.Show(WindowManager, "错误", "删除文件失败,可能是某个文件被占用了。");
        }
        ReLoad();
    }

    private void DelMissionPackFile(XNAMessageBox messageBox)
    {
        
        if (messageBox.Tag != null)
        {
            var missionPack = (Mod)messageBox.Tag;
            try
            {
                Directory.Delete(((MissionPack)ListBoxModAi.SelectedItem.Tag).FilePath, true);
                Directory.Delete(missionPack.FilePath, true);
                File.Delete(missionPack.FileName);
            }
            catch
            {
                XNAMessageBox.Show(WindowManager, "错误", "删除文件失败,可能是某个文件被占用了。");
            }
            
        }
        ReLoad();
    }

    private void BtnReturn_LeftClick(object sender, EventArgs e)
    {
        //CampaignSelector.GetInstance().ScreenMission();
        MyEvent?.Invoke(this, EventArgs.Empty);
        ListBoxModAi.Clear();
        _mcListBoxInfo.ClearItems();
        Parent.RemoveChild(this);
        Disable();
    }

    private void McListBoxInfoSelectedIndexChanged(object sender, EventArgs e)
    {
        if (_mcListBoxInfo.SelectedIndex == -1)
        {
            return;
        }

        var text = _mcListBoxInfo.GetItem(1, _mcListBoxInfo.SelectedIndex).Text;

        _tooltip.Dispose();
        _tooltip.Blocked = true;
        _mcListBoxInfo.RemoveChild(_tooltip);
        _tooltip = new ToolTip(WindowManager, _mcListBoxInfo);
        if (!string.IsNullOrEmpty(text))
            _tooltip.Text = text;
        _mcListBoxInfo.OnMouseLeave();
        _mcListBoxInfo.OnMouseEnter();

    }

    private void ListBoxModAISelectedIndexChanged(object sender, EventArgs e)
    {
        LoadModInfo();
    }

    private void LoadModInfo()
    {
        _mcListBoxInfo.ClearItems();
        Dictionary<string, string> properties = null;
        if (ListBoxModAi.SelectedItem==null)
            return;
        switch (DDModAI.SelectedIndex)
        {
            //筛选Mod
            case 0:
                var mod = Mod.Mods.Find(m => m.ID == ((Mod)ListBoxModAi.SelectedItem.Tag).ID);
                properties = mod.GetProperties();
                BtnNew.Text = "导入新Mod";
                BtnDel.Text = "删除Mod";
                BtnNew.Enable();
                BtnDel.Enable();
                break;
            //筛选AI
            case 1:
                var ai = AI.AIs.Find(m => m.ID == ((AI)ListBoxModAi.SelectedItem.Tag).ID);
                properties = ai.GetProperties();
                BtnNew.Disable();
                BtnDel.Disable();
                break;
            //筛选任务包
            case 2:
                var missionPack = MissionPack.MissionPacks.Find(m => m.ID == ((MissionPack)ListBoxModAi.SelectedItem.Tag).ID);

                properties = missionPack.GetProperties();

                BtnNew.Text = "导入任务包";
                BtnDel.Text = "删除任务包";
                BtnNew.Enable();
                BtnDel.Enable();
                break;

        }

        if (properties != null)
        {
            foreach (var property in properties)
            {
                _mcListBoxInfo.AddItem(new[] { new XNAListBoxItem(property.Key), new XNAListBoxItem(property.Value) });
            }
        }

    }

    private ModManager(WindowManager windowManager) : base(windowManager) { }

    public static ModManager GetInstance(WindowManager windowManager)
    {
        if (_instance != null) return _instance;

        _instance = new ModManager(windowManager);
        _instance.Initialize();
        return _instance;
    }


}

public class ModInfoWindows : XNAWindow
{

    private const int CtbW = 130;
    private const int CtbH = 25;

    private readonly string _title;

    private XNATextBox _ctbModID;
    private XNATextBox _ctbModName;
    private XNATextBox _ctbAuthor;
    private XNATextBox _ctbModDescription;
    private XNATextBox _ctbVersion;
    private XNATextBox _ctbModPath;
    private XNATextBox _ctbCountries;
    private XNACheckBox _chkMutil;
    private XNACheckBox _chkCsf;

    private XNATextBox _ctbCp;

    private XNACheckBox _chkExtensionOn;
    private XNATextBox _ctbExtension;
    private bool _cancel = false;

    private Mod _mod;

    public ModInfoWindows(WindowManager windowManager,Mod mod,string Title) : base(windowManager)
    {
        _mod = mod;
        _title = Title;
    }

    public override void Initialize()
    {
        base.Initialize();
        // Disable();
        //Console.WriteLine(_mod.ToString());
        ClientRectangle = new Rectangle(0, 0, 550, 400);
        CenterOnParent();

        var _lblTitle = new XNALabel(WindowManager)
        {
            ClientRectangle = new Rectangle(230,20,0,0)
            
        };
        AddChild(_lblTitle);

        //第一行
        var lblModID = new XNALabel(WindowManager)
        {
            Text = "ModID(唯一):",
            ClientRectangle = new Rectangle(20, 60, 0, 0)
            
        };
        AddChild(lblModID);

        _ctbModID = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblModID.Right + 100, lblModID.Y, CtbW, CtbH)
        };
        AddChild(_ctbModID);

        var lblModName = new XNALabel(WindowManager)
        {
            Text = "Mod名称:",
            ClientRectangle = new Rectangle(300, lblModID.Y, 0, 0)
        };
        AddChild(lblModName);


        _ctbModName = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblModName.Right + 100, lblModName.Y, CtbW, CtbH)
        };
        AddChild(_ctbModName);

        var lblAuthor = new XNALabel(WindowManager)
        {
            Text = "Mod作者:",
            ClientRectangle = new Rectangle(lblModID.X, 100, 0, 0)
        };
        AddChild(lblAuthor);

        _ctbAuthor = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblAuthor.Right + 100, lblAuthor.Y, CtbW, CtbH)
        };
        AddChild(_ctbAuthor);

        var lblVersion = new XNALabel(WindowManager)
        {
            Text = "Mod版本:",
            ClientRectangle = new Rectangle(lblModName.X, lblAuthor.Y, 0, 0)
        };
        AddChild(lblVersion);

        _ctbVersion = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblVersion.Right + 100, lblVersion.Y, CtbW, CtbH)
        };
        AddChild(_ctbVersion);

        //第二行
        var lblDescription = new XNALabel(WindowManager)
        {
            Text = "Mod介绍:",
            ClientRectangle = new Rectangle(lblModID.X, 140, 0, 0)
        };
        AddChild(lblDescription);

        _ctbModDescription = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblDescription.Right + 100, lblDescription.Y, CtbW, CtbH)
        };
        AddChild(_ctbModDescription);

        var lblCountries = new XNALabel(WindowManager)
        {
            Text = "Mod国家:",
            ClientRectangle = new Rectangle(lblModName.X, lblDescription.Y, 0, 0)
        };
        AddChild(lblCountries);

        _ctbCountries = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblCountries.Right + 100, lblCountries.Y, CtbW, CtbH)
        };
        AddChild(_ctbCountries);

        //第三行
        var lblModPath = new XNALabel(WindowManager)
        {
            Text = "Mod路径:",
            ClientRectangle = new Rectangle(lblModID.X, 180, 0, 0),
            Visible = false
        };
        AddChild(lblModPath);

        _ctbModPath = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblModPath.Right + 100, lblModPath.Y, CtbW, CtbH),
            Visible = false
        };
        AddChild(_ctbModPath);

        _chkMutil = new XNACheckBox(WindowManager)
        {
            Text = "可在遭遇战中使用",
            ClientRectangle = new Rectangle(lblCountries.X, _ctbModPath.Y, 0, 0),
            Checked = true
        };
        AddChild(_chkMutil);


        //第四行
        var lblCp = new XNALabel(WindowManager)
        {
            Text = "兼容战役",
            ClientRectangle = new Rectangle(lblModID.X, 220, 0, 0)
        };
        AddChild(lblCp);

        _ctbCp = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblCp.X+100, lblCp.Y, CtbW, CtbH)
        };


        AddChild(_ctbCp);

        _chkExtensionOn = new XNACheckBox(WindowManager)
        {
            Text = "必须启用扩展",
            ClientRectangle = new Rectangle(_chkMutil.X, _ctbCp.Y, 0, 0),
            Visible = false,
        };
        AddChild(_chkExtensionOn);

        var lblExtension = new XNALabel(WindowManager)
        {
            Text = "支持的扩展",
            ClientRectangle = new Rectangle(lblCp.X, 260, 0, 0)
        };
        AddChild(lblExtension);

        _ctbExtension = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblExtension.Right + 100, lblExtension.Y, CtbW, CtbH)
        };
        AddChild(_ctbExtension);

        _chkCsf = new XNACheckBox(WindowManager)
        {
            Text = "转换为简体中文",
            ClientRectangle = new Rectangle(_chkExtensionOn.X, lblExtension.Y, 0, 0)
        };
        AddChild(_chkCsf);

        var btnOk = new XNAClientButton(WindowManager)
        {
            Text = "确定",
            ClientRectangle = new Rectangle(150,330, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        AddChild(btnOk);
        btnOk.LeftClick += (_, _) =>
        {
            if (Mod.QueryID(_ctbModID.Text) &&_title == "导入Mod")
                XNAMessageBox.Show(WindowManager,"错误", "该ModID已存在。");
            else
                Disable();
        };

        var btnCancel = new XNAClientButton(WindowManager)
        {
            Text = "取消",
            ClientRectangle = new Rectangle(310, 330, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        AddChild(btnCancel);
        btnCancel.LeftClick += (_, _) =>
        {
            _cancel = true;
            Disable();
        };

        _lblTitle.Text = _title;
        _ctbCp.Text = _mod.Compatible;
        _ctbModID.Text = _mod.ID;
        _ctbModName.Text = _mod.Name;
        _ctbModDescription.Text = _mod.Description;
        _ctbVersion.Text = _mod.Version;
        _ctbModPath.Text = _mod.FilePath??$"Mod&AI/Mod/{_mod.ID}";
        _ctbCountries.Text = _mod.Countries;
        _ctbExtension.Text = _mod.Extension;
        _ctbAuthor.Text = _mod.Author;
        _chkCsf.Checked = UserINISettings.Instance.SimplifiedCSF;
    }

    public bool GetCsf()
    {
        return _chkCsf.Checked && _chkCsf.Visible;
    }

    /// <summary>
    /// 获取Mod更新后的信息。
    /// </summary>
    /// <returns></returns>
    public Mod GetModInfo()
    {
        if (_cancel)
            return null;

        _mod.ID = _ctbModID.Text.Trim(); //id
       _mod.Name = _ctbModName.Text.Trim(); //名称
       _mod.Description = _ctbModDescription.Text.Trim(); //介绍
       _mod.Version = _ctbVersion.Text.Trim(); //版本号
       _mod.FilePath = $"Mod&AI/Mod/{_ctbModID.Text.Trim()}"; //路径
       _mod.Compatible = _ctbCp.Text.Trim(); //兼容的战役
        _mod.Countries = _ctbCountries.Text.Trim(); //国家
       _mod.MuVisible = _chkMutil.Checked; //遭遇战可用
       //_mod.CpVisible = CpVisible.SelectedIndex != 0; //兼容的战役
   //    _mod.ExtensionOn = _mod.ExtensionOn; //必须启用扩展
       _mod.Extension = _ctbExtension.Text.Trim(); // 可使用的扩展
       _mod.Author = _ctbAuthor.Text.Trim();
        return _mod;

    }
}

public class AIInfoWindows : XNAWindow
{

    private const int CtbW = 130;
    private const int CtbH = 25;

    private readonly string _title;

    private XNATextBox _ctbAIID;
    private XNATextBox _ctbAIName;
    private XNATextBox _ctbAuthor;
    private XNATextBox _ctbAIDescription;
    private XNATextBox _ctbCompatible;
    private XNATextBox _ctbVersion;
    private XNATextBox _ctbAIPath;
    private XNACheckBox _chkMutil;
    private bool _cancel = false;

    private AI _ai;

    public AIInfoWindows(WindowManager windowManager, AI ai, string Title) : base(windowManager)
    {
        _ai = ai;
        _title = Title;
    }

    public override void Initialize()
    {
        base.Initialize();
        // Disable();
        //Console.WriteLine(_ai.ToString());
        ClientRectangle = new Rectangle(0, 0, 550, 400);
        CenterOnParent();

        var _lblTitle = new XNALabel(WindowManager)
        {
            ClientRectangle = new Rectangle(230, 20, 0, 0)

        };
        AddChild(_lblTitle);

        //第一行
        var lblAIID = new XNALabel(WindowManager)
        {
            Text = "AIID(唯一):",
            ClientRectangle = new Rectangle(20, 60, 0, 0)

        };
        AddChild(lblAIID);

        _ctbAIID = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblAIID.Right + 100, lblAIID.Y, CtbW, CtbH)
        };
        AddChild(_ctbAIID);

        var lblAIName = new XNALabel(WindowManager)
        {
            Text = "AI名称:",
            ClientRectangle = new Rectangle(300, lblAIID.Y, 0, 0)
        };
        AddChild(lblAIName);


        _ctbAIName = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblAIName.Right + 100, lblAIName.Y, CtbW, CtbH)
        };
        AddChild(_ctbAIName);

        var lblAuthor = new XNALabel(WindowManager)
        {
            Text = "AI作者:",
            ClientRectangle = new Rectangle(lblAIID.X, 100, 0, 0)
        };
        AddChild(lblAuthor);

        _ctbAuthor = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblAuthor.Right + 100, lblAuthor.Y, CtbW, CtbH)
        };
        AddChild(_ctbAuthor);

        //第二行
        var lblDescription = new XNALabel(WindowManager)
        {
            Text = "AI介绍:",
            ClientRectangle = new Rectangle(lblAIID.X, 140, 0, 0)
        };
        AddChild(lblDescription);

        _ctbAIDescription = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblDescription.Right + 100, lblDescription.Y, CtbW, CtbH)
        };
        AddChild(_ctbAIDescription);


        var lblVersion = new XNALabel(WindowManager)
        {
            Text = "AI版本:",
            ClientRectangle = new Rectangle(lblAIName.X, lblDescription.Y, 0, 0)
        };
        AddChild(lblVersion);

        _ctbVersion = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblVersion.Right + 100, lblVersion.Y, CtbW, CtbH)
        };
        AddChild(_ctbVersion);


        //第三行
        var lblAIPath = new XNALabel(WindowManager)
        {
            Text = "AI路径:",
            ClientRectangle = new Rectangle(lblAIID.X, 180, 0, 0)
        };
        AddChild(lblAIPath);

        _ctbAIPath = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblAIPath.Right + 100, lblAIPath.Y, CtbW, CtbH)
        };
        AddChild(_ctbAIPath);

        var lblModUse = new XNALabel(WindowManager)
        {
            Text = "可使用的Mod:",
            ClientRectangle = new Rectangle(lblVersion.X, lblAIPath.Y, 0, 0)
        };
        AddChild(lblModUse);

        _ctbCompatible = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblModUse.Right + 100, lblModUse.Y, CtbW, CtbH)
        };
        AddChild(_ctbCompatible);

        var btnOk = new XNAClientButton(WindowManager)
        {
            Text = "确定",
            ClientRectangle = new Rectangle(150, 330, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        AddChild(btnOk);
        btnOk.LeftClick += (_, _) =>
        {
                Disable();
        };

        var btnCancel = new XNAClientButton(WindowManager)
        {
            Text = "取消",
            ClientRectangle = new Rectangle(310, 330, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        AddChild(btnCancel);
        btnCancel.LeftClick += (_, _) =>
        {
            _cancel = true;
            Disable();
        };

        _lblTitle.Text = _title;
        _ctbAIID.Text = _ai.ID;
        _ctbAIName.Text = _ai.Name;
        _ctbAIDescription.Text = _ai.Description;
        //_chkMutil.Checked = _ai.MuVisible;
        _ctbVersion.Text = _ai.Version;
        _ctbAIPath.Text = _ai.FilePath;
        _ctbCompatible.Text = _ai.Compatible;
        // Console.WriteLine(_ai.Author);
        _ctbAuthor.Text = _ai.Author;
    }


    /// <summary>
    /// 获取AI更新后的信息。
    /// </summary>
    /// <returns></returns>
    public AI GetAIInfo()
    {
        if (_cancel)
            return null;

        _ai.ID = _ctbAIID.Text.Trim(); //id
        _ai.Name = _ctbAIName.Text.Trim(); //名称
        _ai.Description = _ctbAIDescription.Text.Trim(); //介绍
        _ai.Version = _ctbVersion.Text.Trim(); //版本号
        _ai.FilePath = _ctbAIPath.Text.Trim(); //路径
        _ai.Compatible = _ctbCompatible.Text.Trim();
        //_ai.MuVisible = _chkMutil.Checked; //遭遇战可用
        _ai.Author = _ctbAuthor.Text.Trim();
        return _ai;

    }
}

public class MissionPackInfoWindows : XNAWindow
{

    private const int CtbW = 130;
    private const int CtbH = 25;

    private readonly string _title;

    private XNATextBox _ctbMissionPackID;
    private XNATextBox _ctbMissionPackName;
    private XNATextBox _ctbAuthor;
    private XNATextBox _ctbMissionPackDescription;
    private XNATextBox _ctbMissionPackPath;
    private XNACheckBox _chkRender;
    private XNACheckBox _chkCsf;
    private XNATextBox _ctbMissionCount;
    private XNATextBox _ctbCp;

    private XNACheckBox _chkExtensionOn;
    private XNATextBox _ctbExtension;
    private bool _cancel = false;
    public bool missionMix;
    public bool csfExist;
    private MissionPack _pack;
    public int missionCount;
    public MissionPackInfoWindows(WindowManager windowManager, MissionPack pack, string title, bool missionMix, bool csfExist) : base(windowManager)
    {
        _pack = pack;
        _title = title;
        this.missionMix = missionMix;
        this.csfExist = csfExist;
    }

    public override void Initialize()
    {
        base.Initialize();
        // Disable();
        //Console.WriteLine(map.ToString());
        ClientRectangle = new Rectangle(0, 0, 550, 400);
        CenterOnParent();

        var _lblTitle = new XNALabel(WindowManager)
        {
            ClientRectangle = new Rectangle(230, 20, 0, 0)

        };
        AddChild(_lblTitle);

        //第一行
        var lblMissionPackID = new XNALabel(WindowManager)
        {
            Text = "任务包ID(唯一):",
            ClientRectangle = new Rectangle(20, 60, 0, 0)

        };
        AddChild(lblMissionPackID);

        _ctbMissionPackID = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblMissionPackID.Right + 100, lblMissionPackID.Y, CtbW, CtbH)
        };
        AddChild(_ctbMissionPackID);

        var lblMissionPackName = new XNALabel(WindowManager)
        {
            Text = "任务包名称:",
            ClientRectangle = new Rectangle(300, lblMissionPackID.Y, 0, 0)
        };
        AddChild(lblMissionPackName);


        _ctbMissionPackName = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblMissionPackName.Right + 100, lblMissionPackName.Y, CtbW, CtbH)
        };
        AddChild(_ctbMissionPackName);

        var lblAuthor = new XNALabel(WindowManager)
        {
            Text = "任务包作者:",
            ClientRectangle = new Rectangle(lblMissionPackID.X, 100, 0, 0)
        };
        AddChild(lblAuthor);

        _ctbAuthor = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblAuthor.Right + 100, lblAuthor.Y, CtbW, CtbH)
        };
        AddChild(_ctbAuthor);

        var lblMissionCount = new XNALabel(WindowManager)
        {
            Text = "任务关数",
            ClientRectangle = new Rectangle(lblMissionPackName.X, 100, 0, 0),
            Visible = false
        };
        AddChild(lblMissionCount);

        _ctbMissionCount = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblMissionCount.Right + 100, lblMissionCount.Y, CtbW, CtbH),
            Visible = false
        };

         AddChild(_ctbMissionCount);

        var lblDescription = new XNALabel(WindowManager)
        {
            Text = "任务包介绍:",
            ClientRectangle = new Rectangle(lblMissionPackID.X, 140, 0, 0)
        };
        AddChild(lblDescription);

        _ctbMissionPackDescription = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblDescription.Right + 100, lblDescription.Y, CtbW, CtbH)
        };
        AddChild(_ctbMissionPackDescription);


        //第三行
        var lblMissionPackPath = new XNALabel(WindowManager)
        {
            Text = "任务包路径:",
            ClientRectangle = new Rectangle(lblMissionPackID.X, 180, 0, 0)
        };
        AddChild(lblMissionPackPath);

        _ctbMissionPackPath = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblMissionPackPath.Right + 100, lblMissionPackPath.Y, CtbW, CtbH)
        };
        AddChild(_ctbMissionPackPath);

        _chkRender = new XNACheckBox(WindowManager)
        {
            Text = "渲染预览图",
            ClientRectangle = new Rectangle(lblMissionPackName.X, _ctbMissionPackPath.Y, 0, 0),
            Checked = true
        };
        AddChild(_chkRender);

        //第四行
        var lblCp = new XNALabel(WindowManager)
        {
            Text = "使用的Mod:",
            ClientRectangle = new Rectangle(lblMissionPackID.X, 220, 0, 0)
        };
        AddChild(lblCp);

        _ctbCp = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblCp.X + 100, lblCp.Y, CtbW, CtbH)
        };

        AddChild(_ctbCp);

        _chkCsf = new XNACheckBox(WindowManager)
        {
            Text = "转换为简体中文",
            ClientRectangle = new Rectangle(lblMissionPackName.X, _ctbCp.Y, 0, 0),
            Checked = true
        };
        AddChild(_chkCsf);

        var lblExtension = new XNALabel(WindowManager)
        {
            Text = "支持的扩展:",
            ClientRectangle = new Rectangle(lblCp.X, 260, 0, 0)
        };
        AddChild(lblExtension);

        _ctbExtension = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblExtension.Right + 100, lblExtension.Y, CtbW, CtbH)
        };
        AddChild(_ctbExtension);


        _chkExtensionOn = new XNACheckBox(WindowManager)
        {
            Text = "必须启用扩展",
            ClientRectangle = new Rectangle(lblMissionPackName.X, lblExtension.Y, 0, 0)
        };
        AddChild(_chkExtensionOn);

        var btnOk = new XNAClientButton(WindowManager)
        {
            Text = "确定",
            ClientRectangle = new Rectangle(150, 330, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        AddChild(btnOk);

        

        btnOk.LeftClick += (_, _) =>
        {

            if (MissionPack.QueryID(_pack.ID) && _title == "导入任务包")
            {
                XNAMessageBox.Show(WindowManager, "错误", "该MissionPackID已存在。");
                return;
            }
            if (missionMix)
            {
                if (string.IsNullOrEmpty(_ctbMissionCount?.Text))
                {
                    XNAMessageBox.Show(WindowManager, "错误", "我们无法推测这个任务包有几关，你需要指定一下");
                    return;
                }

                if (!int.TryParse(_ctbMissionCount.Text, out missionCount))
                {
                    XNAMessageBox.Show(WindowManager, "错误", "任务关数只能输入数字。");
                    return;
                }
            }
            Disable();
        };

        var btnCancel = new XNAClientButton(WindowManager)
        {
            Text = "取消",
            ClientRectangle = new Rectangle(310, 330, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        AddChild(btnCancel);
        btnCancel.LeftClick += (_, _) =>
        {
            _cancel = true;
            Disable();
        };

        _lblTitle.Text = _title;
        _ctbMissionPackID.Text = _pack.ID;
        _ctbMissionPackName.Text = _pack.Name;
        _ctbMissionPackDescription.Text = _pack.Name;
        _ctbMissionPackPath.Text = _pack.FilePath;
        _ctbExtension.Text = _pack.Extension;
        _ctbCp.Text = _pack.Mod;
        // Console.WriteLine(map.Author);
        _ctbAuthor.Text = _pack.Author;
        _chkRender.Checked = UserINISettings.Instance.RenderPreviewImage;
        _chkCsf.Checked = UserINISettings.Instance.SimplifiedCSF;


        if(missionMix)
            _chkRender.Visible = false;

        if (!csfExist)
            _chkCsf.Visible = false;

        if (_title == "修改任务包")
        {
            _chkRender.Text = "重新渲染预览图";
            _chkRender.Checked = false;
            _chkCsf.Text = "重新转换为简体中文";
            _chkCsf.Checked = false;
        }

    }

    public bool GetCsf()
    {
        return _chkCsf.Checked && _chkCsf.Visible;
    }

    public bool GetRander()
    {
        return _chkRender.Checked && _chkRender.Visible;
    }

    /// <summary>
    /// 获取MissionPack更新后的信息。
    /// </summary>
    /// <returns></returns>
    public MissionPack GetMissionPackInfo()
    {
        if (_cancel)
            return null;

        _pack.ID = _ctbMissionPackID.Text.Trim(); //id
        _pack.Name = _ctbMissionPackName.Text.Trim(); //名称
        _pack.Description = _ctbMissionPackDescription.Text.Trim(); //介绍
        _pack.Extension = _ctbExtension.Text.Trim(); // 可使用的扩展
        _pack.FilePath = _ctbMissionPackPath.Text.Trim();
        _pack.Author = _ctbAuthor.Text.Trim();
        _pack.LongDescription = _ctbMissionPackDescription.Text.Trim();
        _pack.Mod = _ctbCp.Text.Trim();
        _pack.MuExtension = _chkExtensionOn.Checked;
        return _pack;

    }
}

public class EditCSFWindows : XNAWindow
{
    public EditCSFWindows(WindowManager windowManager, ToolTip _tooltip,CSF _csf) : base(windowManager)
    {
        this.windowManager = windowManager;
        this._tooltip = _tooltip;
        this._csf = _csf;
        _csfDictionary = _csf.GetCsfDictionary();
    }
    private WindowManager windowManager;
    private XNASuggestionTextBox _tbSearch;
    private XNAMultiColumnListBox _mcListBoxCsfInfo;
    private ToolTip _tooltip;
    private CSF _csf;
    private Dictionary<string,string> _csfDictionary;
    private XNAContextMenu _menu;

    private void DelCsf(string key)
    {
        _csfDictionary.Remove(key);
        Reload();
    }

    private void Reload()
    {   if (_csfDictionary == null)
            return;
        _mcListBoxCsfInfo.ClearItems();
        if (!string.IsNullOrEmpty(_tbSearch.Text) && _tbSearch.Text != "搜索键或值")
            foreach (var (k, v) in _csfDictionary)
            {
                if (k.Contains(_tbSearch.Text) || v.Contains(_tbSearch.Text))
                {
                    _mcListBoxCsfInfo.AddItem(new[] { new XNAListBoxItem(k), new XNAListBoxItem(v) });
                }
            }
        else
            foreach (var (k, v) in _csfDictionary)
            {
                _mcListBoxCsfInfo.AddItem(new[] { new XNAListBoxItem(k), new XNAListBoxItem(v) });
            }
    }

    public override void Initialize()
    {
        _tbSearch = new XNASuggestionTextBox(windowManager);
        _tbSearch.ClientRectangle = new Rectangle(12, 12, 210, 25);
        _tbSearch.Suggestion = "搜索键或值";

        _tbSearch.TextChanged += (_,_) => { Reload(); };

        _mcListBoxCsfInfo = new XNAMultiColumnListBox(windowManager);
        _mcListBoxCsfInfo.ClientRectangle = new Rectangle(12, _tbSearch.Bottom + 12, 320, 250);

        _mcListBoxCsfInfo.AddColumn("键", 120);
        _mcListBoxCsfInfo.AddColumn("值", 200);

        _mcListBoxCsfInfo.SelectedIndexChanged += McListBoxCsfInfoSelectedIndexChanged;
        _mcListBoxCsfInfo.RightClick += McListBoxCsfInfoRightClick;

        AddChild(_tbSearch);
        AddChild(_mcListBoxCsfInfo);

        ClientRectangle = new Rectangle(0, 0, _mcListBoxCsfInfo.Right + 48,_mcListBoxCsfInfo.Bottom + 50);
        WindowManager.CenterControlOnScreen(this);

        var btnSave = new XNAClientButton(windowManager)
        {
            Text = "保存",
            ClientRectangle = new Rectangle(12, _mcListBoxCsfInfo.Bottom + 12, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        btnSave.LeftClick += Save;
        AddChild(btnSave);

        var btnCancel = new XNAClientButton(windowManager)
        {
            Text = "取消",
            ClientRectangle = new Rectangle(btnSave.Right + 12, _mcListBoxCsfInfo.Bottom + 12, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT)
        };
        btnCancel.LeftClick += (_,_)=> Disable();
        AddChild(btnCancel);

        _menu = new XNAContextMenu(windowManager);
        _menu.Name = nameof(_menu);
        _menu.Width = 100;

        _menu.AddItem(new XNAContextMenuItem
        {
            Text = "添加条目",
            SelectAction=Add
        });
        //修改
        _menu.AddItem(new XNAContextMenuItem
        {
            Text = "修改这个条目",
            SelectAction = Edit
        });
        _menu.AddItem(new XNAContextMenuItem
        {
            Text = "删除这个条目",
            SelectAction =Del
        });

        
        AddChild(_menu);

        base.Initialize();
    }

    private void Edit()
    {
        var key = _mcListBoxCsfInfo.GetItem(0, _mcListBoxCsfInfo.SelectedIndex).Text;
        var value = _mcListBoxCsfInfo.GetItem(1, _mcListBoxCsfInfo.SelectedIndex).Text;
        AddCsfWindows addCsfWindows = new AddCsfWindows(windowManager, _csfDictionary, key,value);
        addCsfWindows._reload += Reload;
        addCsfWindows.Show();
    }

    private void Add()
    {
        AddCsfWindows addCsfWindows = new AddCsfWindows(windowManager, _csfDictionary,"","");
        addCsfWindows._reload += Reload;
        addCsfWindows.Show();
    }

    private void Del()
    {
        var key = _mcListBoxCsfInfo.GetItem(0, _mcListBoxCsfInfo.SelectedIndex).Text;
        var message = new XNAMessageBox(windowManager, "删除确认", "确定要删除这个条目吗？", XNAMessageBoxButtons.YesNo);
        message.YesClickedAction += (_) => DelCsf(key);
        message.Show();
    }

    private void McListBoxCsfInfoRightClick(object sender, EventArgs e)
    {
        _mcListBoxCsfInfo.SelectedIndex = _mcListBoxCsfInfo.HoveredIndex;



        _menu.Open(GetCursorPoint());
    }

    private void McListBoxCsfInfoSelectedIndexChanged(object sender, EventArgs e)
    {
        if(_mcListBoxCsfInfo.SelectedIndex == -1)
        {
            return;
        }

        var text = _mcListBoxCsfInfo.GetItem(1, _mcListBoxCsfInfo.SelectedIndex).Text;

        _tooltip.Dispose();
        _tooltip.Blocked = true;
        _mcListBoxCsfInfo.RemoveChild(_tooltip);
        _tooltip = new ToolTip(WindowManager, _mcListBoxCsfInfo);
        if (!string.IsNullOrEmpty(text))
            _tooltip.Text = text;
        _mcListBoxCsfInfo.OnMouseLeave();
        _mcListBoxCsfInfo.OnMouseEnter();

    }

    private void Save(object sender, EventArgs e)
    {
        CSF.WriteCSF(_csfDictionary,_csf.csfPath);
        Disable();
    }

    public void Show()
    {
        DarkeningPanel.AddAndInitializeWithControl(WindowManager, this);
    }

}

public class AddCsfWindows : XNAWindow
{
    public AddCsfWindows(WindowManager windowManager,Dictionary<string,string> csfDictionary,string key,string value) : base(windowManager) {
        _csfDictionary = csfDictionary;
        _key = key;
        _value = value;
    }
    private Dictionary<string, string> _csfDictionary;
    private string _key;
    private string _value;
    private XNATextBox _tbKey;
    private XNATextBox _tbValue;
    public delegate void ReloadDelegate();
    public event ReloadDelegate _reload;
    public override void Initialize()
    {
        base.Initialize();

        var lblKey = new XNALabel(WindowManager);
        lblKey.Text = "键";
        lblKey.ClientRectangle = new Rectangle(20, 20, 0, 0);

        _tbKey = new XNATextBox(WindowManager);
        _tbKey.ClientRectangle = new Rectangle(lblKey.Right + 20, lblKey.Y, 150, 25);
        _tbKey.Text = _key;

        var lblValue = new XNALabel(WindowManager);
        lblValue.Text = "值";
        lblValue.ClientRectangle = new Rectangle(20, _tbKey.Bottom + 20, 0, 0);

        _tbValue = new XNATextBox(WindowManager);
        _tbValue.ClientRectangle = new Rectangle(lblValue.Right + 20, lblValue.Y, 150, 25);
        _tbValue.Text = _value;

        var btnAdd = new XNAClientButton(WindowManager);
        btnAdd.Text = "添加";
        if(!string.IsNullOrEmpty(_key)|| !string.IsNullOrEmpty(_value))
            btnAdd.Text = "修改";
        btnAdd.ClientRectangle = new Rectangle(20, _tbValue.Bottom + 20, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
        btnAdd.LeftClick += Add;
        AddChild(btnAdd);

        var btnCancel = new XNAClientButton(WindowManager);
        btnCancel.Text = "取消";
        btnCancel.ClientRectangle = new Rectangle(btnAdd.Right + 12, _tbValue.Bottom + 20, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
        btnCancel.LeftClick += (_, _) => Disable();
        AddChild(btnCancel);

        AddChild(lblKey);
        AddChild(_tbKey);
        AddChild(lblValue);
        AddChild(_tbValue);

        ClientRectangle = new Rectangle(0, 0, 300, btnCancel.Bottom + 20);
        WindowManager.CenterControlOnScreen(this);
    }

    private void Add(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_tbKey.Text))
        {
            XNAMessageBox.Show(WindowManager, "错误", "键不能为空");
            return;
        }

        _csfDictionary[_tbKey.Text] = _tbValue.Text ;
        _reload?.Invoke();
        Disable();
    }

    public void Show()
    {
        DarkeningPanel.AddAndInitializeWithControl(WindowManager, this);
    }
}