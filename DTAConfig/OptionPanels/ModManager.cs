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
using System.Reflection;
using System.Xml.Linq;
using Microsoft.Xna.Framework.Input;

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

    private XNAContextMenu _modMenu;
    public Action 触发刷新;

    public override void Initialize()
    {
        base.Initialize();
        
        ClientRectangle = new Rectangle(0, 0, 750, 384);
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
            ClientRectangle = new Rectangle(25, 30, 200, 40)
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
        //_modMenu.AddItem(new XNAContextMenuItem
        //{
        //    Text = "修改",
        //    SelectAction = UpdateBase
        //});
        _modMenu.AddItem(new XNAContextMenuItem
        {
            Text = "删除",
            SelectAction = () => BtnDel.OnLeftClick()
        });
        _modMenu.AddItem(new XNAContextMenuItem 
        { Text = "编辑CSF", 
            SelectAction = EditCsf
        });

        AddChild(_modMenu);

        ListBoxModAi = new XNAListBox(WindowManager)
        {
            ClientRectangle = new Rectangle(DDModAI.X, DDModAI.Y + 40, 260, 260),
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
                _modMenu.Items[2].Visible = false;
                
            }
            else
            {
                _modMenu.Items[2].Visible = true;
            }

            _modMenu.Open(GetCursorPoint());
        };
        AddChild(ListBoxModAi);

        _mcListBoxInfo = new XNAMultiColumnListBox(WindowManager)
        {
            ClientRectangle = new Rectangle(ListBoxModAi.X + ListBoxModAi.Width + 20, ListBoxModAi.Y, 420, ListBoxModAi.Height),
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

     //   EnabledChanged += ModManager_EnabledChanged;

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
    private void 整合Mod文件(string modPath,Mod mod,bool covCsf)
    {
        #region 导入Mod文件

        //提取Mod文件
        if(!Directory.Exists(mod.FilePath))
            Directory.CreateDirectory(mod.FilePath);

        //提取mix文件
        HashSet<string> mixFileExclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Ares.file", "ra2md.file", "ra2.file", "langmd.file", "language.file", "movmd03.file", "multimd.file",
            "movmd01.file", "movmd02.file","NPatch.file","movies01.file","movies02.file","MAPS02.file","MAPS01.file",
            ProgramConstants.CORE_MIX,ProgramConstants.SKIN_MIX,ProgramConstants.MISSION_MIX
        };

        try
        {

            // 复制除了指定列表之外的所有.mix文件，同时处理以"Ecache"或"EXPAND"开头的文件
            int expandCounter = 2; // 统一的扩展文件计数器
            foreach (var mix in Directory.GetFiles(modPath, "*.file"))
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
                        newFileName = $"expandmd{expandCounter++.ToString("D2")}.file";
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
                    }
                    else
                        allFiles.Add(ini);
                }
            }

            //先复制一份原版或者尤复的过去


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

        #endregion
    }

    private static void 整合任务包文件(string MissionPackPath, MissionPack missionPack, bool covCsf)
    {
        if(!Directory.Exists(missionPack.FilePath))
            Directory.CreateDirectory(missionPack.FilePath);

        if (Directory.Exists($"Mod&AI/Mod/{missionPack.ID}"))
            Directory.GetFiles($"Mod&AI/Mod/{missionPack.ID}")
                .ToList()
                .ForEach(file => File.Copy(file, Path.Combine(missionPack.FilePath, Path.GetFileName(file)), true));

        foreach (var file in Directory.GetFiles(MissionPackPath, "*.map"))
            File.Copy(file,Path.Combine(missionPack.FilePath,Path.GetFileName(file)),true);

        foreach (var csf in Directory.GetFiles(MissionPackPath, "*.csf"))
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

        if (File.Exists(Path.Combine(MissionPackPath, "missionmd.ini")))
            File.Copy(Path.Combine(MissionPackPath, "missionmd.ini"), Path.Combine(missionPack.FilePath, "missionmd.ini"),true);
        if (File.Exists(Path.Combine(MissionPackPath, "mapselmd.ini")))
            File.Copy(Path.Combine(MissionPackPath, "mapselmd.ini"), Path.Combine(missionPack.FilePath, "mapselmd.ini"), true);
        if (File.Exists(Path.Combine(MissionPackPath, "game.fnt")))
            File.Copy(Path.Combine(MissionPackPath, "game.fnt"), Path.Combine(missionPack.FilePath, "game.fnt"), true);

        missionPack.Create();
    }

    void 查找并解压压缩包(string zip)
    {
        var zips = Directory.GetFiles(zip, "*.*", SearchOption.AllDirectories)
                         .Where(file => new[] { ".zip", ".rar", ".7z" }.Contains(Path.GetExtension(file).ToLower()))
                         .ToArray();
        if (zips.Length == 0) return;

        foreach (var item in zips)
        {
            SevenZip.ExtractWith7Zip(item, $"./tmp/{Path.GetFileNameWithoutExtension(zip)}/{Path.GetFileNameWithoutExtension(item)}", needDel: true);

            查找并解压压缩包(Path.Combine(Path.GetDirectoryName(item), Path.GetFileNameWithoutExtension(item)));
        }
    }

    /// <summary>
    /// 导入任务包.
    /// </summary>
    public string 导入任务包(string filePath)
    {
        var 后缀 = Path.GetExtension(filePath);
        if (后缀 != ".zip" && 后缀 != ".rar" && 后缀 != ".7z" && 后缀 != ".map" && 后缀 != ".file")
        {
            XNAMessageBox.Show(WindowManager, "错误", "请选择任务包文件");
            return "没有找到任务包文件";
        }

        var path = Path.GetDirectoryName(filePath);
        if (后缀 == ".zip" || 后缀 == ".rar" || 后缀 == ".7z")
        {
            var missionPath = $"{ProgramConstants.GamePath}/tmp/{Path.GetFileNameWithoutExtension(filePath)}";
            SevenZip.ExtractWith7Zip(filePath, missionPath);
            path = missionPath;
        }

        查找并解压压缩包(path);

        List<string> mapFiles = [];

        var id = string.Empty;

        if (Directory.Exists(path))
        {
            List<string> list = [path, .. Directory.GetDirectories(path, "*", SearchOption.AllDirectories)];
            foreach (var item in list)
            {
                if (判断是否为任务包(item))
                {

                    var r = 导入具体任务包(item);
                    if (r != string.Empty)
                    {
                        id = r;
                        mapFiles.AddRange(Directory.GetFiles($"maps/cp/{id}", "*.map"));
                    } 


                }
            }
        }
        // 如果路径本身不符合任务包，才检查其子目录
        
        
        if(id == string.Empty)
        {
            XNAMessageBox.Show(WindowManager, "错误", "请选择任务包文件");
            return "没有找到任务包文件";
        }


        ReLoad();

        //渲染预览图
        if (UserINISettings.Instance.RenderPreviewImage.Value)
            Task.Run(async () =>
            {
                await RenderPreviewImageAsync(mapFiles.ToArray());
            });

        触发刷新?.Invoke();

        return id;
    }

    public string 导入具体任务包(string missionPath)
    {
        bool isYR = 判断是否为尤复(missionPath);

        var id = Path.GetFileName(missionPath);
        var missionPack = new MissionPack
        {
            ID = id,
            FilePath = $"Maps\\CP\\{id}",
            Name = Path.GetFileName(missionPath),
            YR = isYR,
            Other = true,
            LongDescription = Path.GetFileName(missionPath),
            Mod = isYR ? "YR" : "RA2"
        };

        missionPack.DefaultMod = missionPack.Mod;

        if (导入具体Mod(missionPath, isYR, false) == string.Empty) //说明检测到Mod
        {
            missionPack.Mod += "," + id;
            missionPack.DefaultMod = id;
        }

        整合任务包文件(missionPath, missionPack, UserINISettings.Instance.SimplifiedCSF.Value);

        写入任务INI(missionPack);

        return id;
    }

    private void 写入任务INI(MissionPack missionPack)
    {
        var maps = Directory.GetFiles(missionPack.FilePath, "*.map").ToList();
        var md = missionPack.YR ? "md" : string.Empty;

        var battleINI = new IniFile($"Maps\\CP\\battle{missionPack.ID}.ini");
        if (!battleINI.SectionExists("Battles"))
            battleINI.AddSection("Battles");

        //先确定可用的ini
        var mapSelINIPath = "Resources//mapselmd.ini";
        if (File.Exists(Path.Combine(missionPack.FilePath, $"mapsel{md}.ini")))
            mapSelINIPath = Path.Combine(missionPack.FilePath, $"mapsel{md}.ini");

        var missionINIPath = "Resources//missionmd.ini";
        if (File.Exists(Path.Combine(missionPack.FilePath, $"mission{md}.ini")))
            missionINIPath = Path.Combine(missionPack.FilePath, $"mission{md}.ini");

        var csfPath = $"Maps\\CP\\{missionPack.ID}//ra2md.csf";
        var csf = new CSF(csfPath).GetCsfDictionary();

        var missionINI = new IniFile(missionINIPath);
        var mapSelINI = new IniFile(mapSelINIPath);

        if (maps.Count == 0) //如果没有地图
        {
            添加默认地图("GDI");
            添加默认地图("Nod");
        }

        void 添加默认地图(string typeName){
            var GDI = mapSelINI.GetSection(typeName);
            if (GDI == null) return;
            
            var keys = mapSelINI.GetSectionKeys(typeName);
            foreach(var key in keys)
            {
                var sectionName = mapSelINI.GetValue(typeName, key, string.Empty);
                if(sectionName == string.Empty) continue;

                var map = mapSelINI.GetValue(sectionName, "Scenario", string.Empty);
                if (map == string.Empty) continue;

                if(mapSelINIPath == "Resources//mapselmd.ini" && missionINIPath == "Resources//missionmd.ini")
                if ((map.ToLower().EndsWith("md.map") && !missionPack.YR) || !map.ToLower().EndsWith("md.map") && missionPack.YR) continue;

                maps.Add(map);
            }
            
        }

        var count = 1;
        battleINI.SetValue("Battles", missionPack.ID, missionPack.ID);

        foreach (var map in maps)
        {
            var mapName = Path.GetFileName(map);

            var sectionName = missionPack.ID + count;
            
            if (!battleINI.SectionExists(sectionName))
                battleINI.AddSection(sectionName);

            var 阵营 = "";
            if (mapName.ToLower().Contains("all"))
                阵营 = "Allied";
            else if (mapName.ToLower().Contains("sov"))
                阵营 = "Soviet";

            var 任务名称 = csf?.GetValueOrDefault(missionINI.GetValue(mapName, "UIName", string.Empty)) ?? $"第{count}关";//任务名称
            var 任务地点 = csf?.GetValueOrDefault(missionINI.GetValue(mapName, "LSLoadMessage", string.Empty)) ?? ""; //任务地点
            var 任务简报 = csf?.GetValueOrDefault(missionINI.GetValue(mapName, "Briefing", string.Empty)) ?? ""; //任务描述
            var 任务目标 = csf?.GetValueOrDefault(missionINI.GetValue(mapName, "LSLoadBriefing", string.Empty)) ?? ""; //任务目标

            var LongDescription = 任务地点 + "@@" + 任务简报 + "@" + 任务目标;

            battleINI.SetValue("Battles", sectionName, sectionName)
                     .SetValue(sectionName, "Scenario", mapName.ToUpper())
                     .SetValue(sectionName, "Description", 任务名称)
                     .SetValue(sectionName, "LongDescription", LongDescription.Replace("\n", "@"))
                     .SetValue(sectionName, "MissionPack", missionPack.ID)
                     .SetValue(sectionName, "SideName", 阵营)
                     ;
            count++;
        }

        battleINI.WriteIniFile();

    }

    private bool 判断是否为Mod(string path,bool isYR)
    {
        var md = isYR ? "md" : string.Empty;

        var shps = Directory.GetFiles(path, "*.shp")
           .Where(file => !Path.GetFileName(file).StartsWith("ls") && !Path.GetFileName(file).StartsWith("gls"))
           .ToArray();
        var vxls = Directory.GetFiles(path, "*.vxl");
        var pals = Directory.GetFiles(path, "*.pal")
            .Where(file => !Path.GetFileName(file).StartsWith("ls") && !Path.GetFileName(file).StartsWith("gls"))
            .ToArray();

        var mixs = Directory.GetFiles(path, $"expand{md}*.file")
            .ToArray();

        var inis = Directory.GetFiles(path, $"*.ini")
            .Where(file => 
            Path.GetFileName(file) != $"battle{md}.ini" &&
                    Path.GetFileName(file) != $"mapsel{md}.ini" &&
                    Path.GetFileName(file) != $"missionPack{md}.ini" &&
                    Path.GetFileName(file) != $"ai{md}.ini" &&
                    Path.GetFileName(file) != $"mpbattle{md}.ini"
                    )
            .ToArray();

        return shps.Length + vxls.Length + pals.Length + mixs.Length + inis.Length != 0;
    }

    private bool 判断是否为任务包(string path)
    {
        return Directory.Exists(path) && Directory.GetFiles(path, "*.map").Length + Directory.GetFiles(path, "*.file").Length != 0;
    }

    private static bool 判断是否为尤复(string path)
    {
        string[] YRFiles = ["gamemd.exe", "RA2MD.CSF", "expandmd01.file", "rulesmd.ini", "artmd.ini", "glsmd.shp"];
        
        return Directory.Exists(path) && YRFiles.Any(file => File.Exists(Path.Combine(path, file))) || Directory.GetFiles(path, "expandmd*.file").Length != 0 || Directory.GetFiles(path, "*md.map").Length != 0;
    }

    private string 导入Mod(string filePath,bool reload = true)
    {
        var 后缀 = Path.GetExtension(filePath);
        if (后缀 != ".zip" && 后缀 != ".rar" && 后缀 != ".7z" && 后缀 != ".ini" && 后缀 != ".file")
        {
            if(reload)
                XNAMessageBox.Show(WindowManager, "错误", "请选择Mod文件");
            return "请选择任务包文件";
        }

        var path = Path.GetDirectoryName(filePath);
        if (后缀 == ".zip" || 后缀 == ".rar" || 后缀 == ".7z")
        {
            var missionPath = $"./tmp/{Path.GetFileNameWithoutExtension(filePath)}";
            SevenZip.ExtractWith7Zip(filePath, missionPath);
            path = missionPath;
        }

        var id = string.Empty;

        if (Directory.Exists(path))
        {
            List<string> list = [path, .. Directory.GetDirectories(path, "*", SearchOption.AllDirectories)];
            foreach (var item in list)
            {
                if (判断是否为Mod(item, 判断是否为尤复(item)))
                {
                    var r = 导入具体Mod(item, reload);
                    if (r != string.Empty)
                    {
                        id = r;
                    }
                }
                
            }
        }

        if (id == string.Empty)
        {
            if (reload)
                XNAMessageBox.Show(WindowManager, "错误", "请选择Mod文件");
            return "请选择Mod文件";
        }


        if (reload)
        {
            ReLoad();
            触发刷新?.Invoke();
        }

        return id;

    }

    private string 导入具体Mod(string path,bool? isYR = null,bool reload = true)
    {
        isYR ??= 判断是否为尤复(path);
        var md = isYR.GetValueOrDefault() ? "md" : string.Empty;

        if(!判断是否为Mod(path, isYR.GetValueOrDefault())) return "";

        var id = Path.GetFileName(path);

        var mod = new Mod
        {
            ID = id,
            FilePath = $"Mod&AI\\Mod\\{id}",
            Name = Path.GetFileName(path),
            UseAI = isYR.GetValueOrDefault() ? "YRAI" : "RA2AI",
            md = md,
            MuVisible = reload
        };
   
        (mod.Extension, mod.ExtensionOn) = 处理扩展情况(path);

        整合Mod文件(path, mod,UserINISettings.Instance.SimplifiedCSF.Value);

        return id;
    }

    private string 导入AI(string filePath)
    {
        var 后缀 = Path.GetExtension(filePath);
        if (后缀 != ".zip" && 后缀 != ".rar" && 后缀 != ".7z" && 后缀 != ".map" && 后缀 != ".file")
        {
            XNAMessageBox.Show(WindowManager, "错误", "请选择任务包文件");
            return "没有找到任务包文件";
        }

        var path = Path.GetDirectoryName(filePath);
        if (后缀 == ".zip" || 后缀 == ".rar" || 后缀 == ".7z")
        {
            var missionPath = $"{ProgramConstants.GamePath}/tmp/{Path.GetFileNameWithoutExtension(filePath)}";
            SevenZip.ExtractWith7Zip(filePath, missionPath);
            path = missionPath;
        }

        var id = string.Empty;

        查找并解压压缩包(path);
        if (Directory.Exists(path))
        {
            List<string> list = [path, .. Directory.GetDirectories(path, "*", SearchOption.AllDirectories)];
            foreach (var item in list)
            {

                
                if (判断是否为Mod(item, 判断是否为尤复(item)))
                {
                    var r = 导入具体Mod(item, true);
                    if (r != string.Empty)
                    {
                        id = r;
                    }
                }
                else if (判断是否为AI(item))
                {
                    var r = 导入具体AI(item);
                    if (r != string.Empty)
                    {
                        id = r;
                    }
                }
            }

            ReLoad();
            触发刷新?.Invoke();
        }

        return id;
    }

    private string 导入具体AI(string path)
    {
        var isYR = 判断是否为尤复(path);
        var md = isYR ? "md" : string.Empty;

        if (!判断是否为AI(path)) return "";

        var id = Path.GetFileName(path);

        var ai = new AI
        {
            ID = id,
            FilePath = $"Mod&AI\\AI\\{id}",
            Name = Path.GetFileName(path),
            YR = isYR,
            Compatible = isYR ? "YR" : "RA2",
        };

        整合AI文件(path, ai);

        return id;
    }

    private void 整合AI文件(string path, AI ai)
    {
        if (!Directory.Exists(ai.FilePath))
            Directory.CreateDirectory(ai.FilePath);

        foreach (var file in Directory.GetFiles(path))
        {
            File.Copy(file, Path.Combine(ai.FilePath, Path.GetFileName(file)), true);
        }

        ai.Create();

    }

    private bool 判断是否为AI(string path)
    {
        return File.Exists(Path.Combine(path, "aimd.ini")) || File.Exists(Path.Combine(path, "ai.ini"));
    }

    private static (string,bool) 处理扩展情况(string path)
    {
        var (extension, extensionOn) = (string.Empty, false);
        
        string extensionPath = "Mod&AI\\Extension";
        //检测ARES
        if (File.Exists(Path.Combine(path, "Ares.dll")))
        {
            var aresVerison = FileVersionInfo.GetVersionInfo(Path.Combine(path, "Ares.dll")).ProductVersion;

            extensionOn = true;

            //如果用的自带的3.0p1
            if (aresVerison == "3.0p1" || aresVerison == "3.0")
                extension += $"Ares3";
            else
            {
                extension += $"Ares{aresVerison},";
                if (!Directory.Exists($"{extensionPath}\\Ares{aresVerison}"))
                    Directory.CreateDirectory($"{extensionPath}\\Ares\\Ares{aresVerison}");

                File.Copy(Path.Combine(path, "Ares.dll"), $"{extensionPath}\\Ares\\Ares{aresVerison}\\Ares.dll",true);

                if (File.Exists(Path.Combine(path, "Ares.Mix")))
                {
                    File.Copy(Path.Combine(path, "Ares.Mix"), $"{extensionPath}\\Ares\\Ares{aresVerison}\\Ares.Mix", true);
                }
                if (File.Exists(Path.Combine(path, "Syringe.exe")))
                {
                    File.Copy(Path.Combine(path, "Syringe.exe"), $"{extensionPath}\\Ares\\Ares{aresVerison}\\Syringe.exe", true);
                }

            }
        }

       
        //检测Phobos
        if (File.Exists(Path.Combine(path, "Phobos.dll")))
        {
            var phobosVersion = FileVersionInfo.GetVersionInfo(Path.Combine(path, "Phobos.dll")).FileVersion;

            extensionOn = true;
            //如果用的自带的36
            if (phobosVersion != "0.0.0.36")
            {
                extension += $",Phobos{phobosVersion}";
                if (!Directory.Exists($"{extensionPath}\\Phobos{phobosVersion}"))
                    Directory.CreateDirectory($"{extensionPath}\\Phobos\\Phobos{phobosVersion}");
                File.Copy(Path.Combine(path, "phobos.dll"), $"{extensionPath}\\Phobos\\phobos{phobosVersion}\\Phobos.dll", true);

            }
            else
            {
                extension += ",Phobos";
            }
        }

        if (extension == string.Empty)
            extension = $"{ProgramConstants.ARES},{ProgramConstants.PHOBOS}";

        return (extension, extensionOn);
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
            触发刷新?.Invoke();
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
            触发刷新?.Invoke();
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

            if (rander)
            {
                string[] mapFiles = Directory.GetFiles(missionPack.FilePath, "*.map");
                await RenderPreviewImageAsync(mapFiles);
            }
            ReLoad(); //重新载入
            触发刷新?.Invoke();
        };
    }

    private async Task RenderPreviewImageAsync(string[] mapFiles)
    {
        
        if(mapFiles.Length == 0) return;

        //var messageBox = new XNAMessage(WindowManager);

        //messageBox.caption = "渲染中...";
        //messageBox.description = $"已渲染图像 0 / {mapFiles.Length}";
        //messageBox.Show();


        //void RenderCompletedHandler(object sender, EventArgs e)
        //{

        //    messageBox.description = $"已渲染图像 {RenderImage.RenderCount} / {mapFiles.Length}";

        //    if (RenderImage.RenderCount == mapFiles.Length)
        //    {
        //        messageBox.Disable();
        //        // 渲染完成后，解除事件绑定
        //        RenderImage.RenderCompleted -= RenderCompletedHandler;
        //    }
        //}

        //RenderImage.RenderCompleted += RenderCompletedHandler;

   //     RenderImage.RenderImagesAsync();

        RenderImage.需要渲染的地图列表.InsertRange(0,mapFiles);
        UserINISettings.Instance.取消渲染地图();
        _ = RenderImage.RenderImagesAsync();
    }

    private void BtnNew_LeftClick(object sender, EventArgs e)
    {

        //var folderBrowser = new FolderBrowserDialog
        //{
        //    Description = "请选择目录"
        //};
        //if (folderBrowser.ShowDialog() != DialogResult.OK)
        //    return;
        //var missionPath = folderBrowser.SelectedPath;

        var openFileDialog = new OpenFileDialog
        {
            Title = "请选择文件夹或压缩包",
            Filter = "压缩包 (*.zip;*.rar;*.7z;*.map;*.file)|*.zip;*.rar;*.7z;*.map;*.file|所有文件 (*.*)|*.*", // 限制选择的文件类型
            CheckFileExists = true,   // 检查文件是否存在
            ValidateNames = true,     // 验证文件名
            Multiselect = false       // 不允许多选
        };

        if (openFileDialog.ShowDialog() != DialogResult.OK)
            return;

        if (DDModAI.SelectedIndex == 0)
            导入Mod(openFileDialog.FileName);
        if (DDModAI.SelectedIndex == 1)
            导入AI(openFileDialog.FileName);
        if (DDModAI.SelectedIndex == 2)
            导入任务包(openFileDialog.FileName);
    }

    private void ReLoad()
    {
        Mod.reLoad();
       
        MissionPack.reLoad();

        AI.reLoad();

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
            if (ListBoxModAi.SelectedItem.Tag is not MissionPack missionPack) return;

            删除任务包(missionPack);
        }
        else
        {
            if (ListBoxModAi.SelectedItem.Tag is not Mod mod) return;

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
            xNAMessageBox.YesClickedAction += (_) => DelMod(mod) ;
            xNAMessageBox.Show();
        }
    }
     
    public void 删除任务包(MissionPack missionPack)
    {

        if (!missionPack.CanDel)
        {
            XNAMessageBox.Show(WindowManager, "提示", "系统自带模组无法删除");
            return;
        }
        UserINISettings.Instance.取消渲染地图?.Invoke();
        var inifile = new IniFile(missionPack.FileName);
        var m = string.Empty;

        foreach (var s in inifile.GetSections())
        {
            if (inifile.GetValue(s, "MissionPack", string.Empty) == missionPack.ID)
                m += inifile.GetValue(s, "Description", s) + Environment.NewLine;
        }

        var xNAMessageBox = new XNAMessageBox(WindowManager, "删除确认",
            $"您真的要删除任务包{missionPack.Name}吗？它包含以下任务：{Environment.NewLine}{m} ", XNAMessageBoxButtons.YesNo);
        xNAMessageBox.YesClickedAction += (_) => { DelMissionPack(missionPack); };
        xNAMessageBox.Show();
    }

    public void DelMissionPack(MissionPack missionPack)
    {
        var iniFile = new IniFile(missionPack.FileName);
        if (iniFile.GetSection("MissionPack").Keys.Count == 1)
            File.Delete(missionPack.FileName);
        else
        {
            iniFile.RemoveKey("MissionPack", missionPack.ID);
            foreach (var fore in iniFile.GetSections())
            {
                if (iniFile.GetValue(fore, "MissionPack", string.Empty) == missionPack.ID)
                {
                    iniFile.RemoveKey("Battles", fore);
                    iniFile.RemoveSection(fore);
                }
            }
            iniFile.WriteIniFile();
        }

        
        if (!string.IsNullOrEmpty(missionPack.DefaultMod))
        {
            var mod = Mod.Mods.Find(m => m.ID == missionPack.DefaultMod);
            if (mod != null && mod.CanDel)
            {
                DelMod(mod);
            }
        }

        try
        {
            Directory.Delete(missionPack.FilePath, true);
        }
        catch
        {
            XNAMessageBox.Show(WindowManager, "错误", "删除文件失败,可能是某个文件被占用了。");
        }

        ReLoad();
        触发刷新?.Invoke();
        UserINISettings.Instance.开始渲染地图?.Invoke();
    }

    public void DelMod(Mod mod)
    {
        if (!mod.CanDel)
        {
            XNAMessageBox.Show(WindowManager, "提示", "系统自带模组无法删除");
            return;
        }
        UserINISettings.Instance.取消渲染地图?.Invoke();

        var iniFile = new IniFile(mod.FileName);
        if (iniFile.GetSection("Mod").Keys.Count == 1)
            File.Delete(mod.FileName);
        else
        {
            iniFile.RemoveKey("Mod", mod.ID);
            iniFile.RemoveSection(mod.ID);
            iniFile.WriteIniFile();
        }
        try
        {
            Directory.Delete(mod.FilePath, true);
        }
        catch
        {
            XNAMessageBox.Show(WindowManager, "错误", "删除文件失败,可能是某个文件被占用了。");
        }

        ReLoad();
        触发刷新?.Invoke();
        UserINISettings.Instance.开始渲染地图?.Invoke();
    }

    private void BtnReturn_LeftClick(object sender, EventArgs e)
    {
        //CampaignSelector.GetInstance().ScreenMission();
        //触发刷新?.Invoke();
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
                BtnNew.Text = "导入新AI";
                BtnDel.Text = "删除AI";
                BtnNew.Enable();
                BtnDel.Enable();
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

    //private XNATextBox _ctbModID;
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
        //var lblModID = new XNALabel(WindowManager)
        //{
        //    Text = "ModID(唯一):",
        //    ClientRectangle = new Rectangle(20, 60, 0, 0)
            
        //};
        //AddChild(lblModID);

        //_ctbModID = new XNATextBox(WindowManager)
        //{
        //    ClientRectangle = new Rectangle(lblModID.Right + 100, lblModID.Y, CtbW, CtbH)
        //};
        //AddChild(_ctbModID);

        var lblModName = new XNALabel(WindowManager)
        {
            Text = "Mod名称:",
            ClientRectangle = new Rectangle(20, 60, 0, 0)
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
            ClientRectangle = new Rectangle(lblModName.X, 100, 0, 0)
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
            ClientRectangle = new Rectangle(300, lblAuthor.Y, 0, 0)
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
            ClientRectangle = new Rectangle(lblModName.X, 140, 0, 0)
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
            ClientRectangle = new Rectangle(lblVersion.X, lblDescription.Y, 0, 0)
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
            ClientRectangle = new Rectangle(lblModName.X, 180, 0, 0),
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
            ClientRectangle = new Rectangle(lblModName.X, 220, 0, 0)
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
        //_ctbModID.Text = _mod.ID;
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

    //    _mod.ID = _ctbModID.Text.Trim(); //id
      //  _mod.ID = _mod.ID;
       _mod.Name = _ctbModName.Text.Trim(); //名称
       _mod.Description = _ctbModDescription.Text.Trim(); //介绍
       _mod.Version = _ctbVersion.Text.Trim(); //版本号
      // _mod.FilePath = _mod.FilePath; //路径
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
        //var lblMissionPackID = new XNALabel(WindowManager)
        //{
        //    Text = "任务包ID(唯一):",
        //    ClientRectangle = new Rectangle(20, 60, 0, 0)

        //};
        //AddChild(lblMissionPackID);

        //_ctbMissionPackID = new XNATextBox(WindowManager)
        //{
        //    ClientRectangle = new Rectangle(lblMissionPackID.Right + 100, lblMissionPackID.Y, CtbW, CtbH)
        //};
        //AddChild(_ctbMissionPackID);

        var lblMissionPackName = new XNALabel(WindowManager)
        {
            Text = "任务包名称:",
            ClientRectangle = new Rectangle(300, 60, 0, 0)
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
            ClientRectangle = new Rectangle(20, 100, 0, 0)
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
            //Visible = missionMix
            Visible = false
        };
        AddChild(lblMissionCount);

        _ctbMissionCount = new XNATextBox(WindowManager)
        {
            ClientRectangle = new Rectangle(lblMissionCount.Right + 100, lblMissionCount.Y, CtbW, CtbH),
            //Visible = missionMix
            Visible = false
        };

         AddChild(_ctbMissionCount);

        var lblDescription = new XNALabel(WindowManager)
        {
            Text = "任务包介绍:",
            ClientRectangle = new Rectangle(lblAuthor.X, 140, 0, 0)
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
            ClientRectangle = new Rectangle(lblAuthor.X, 180, 0, 0)
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
            ClientRectangle = new Rectangle(lblAuthor.X, 220, 0, 0)
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
            //if (missionMix)
            //{
            //    if (string.IsNullOrEmpty(_ctbMissionCount?.Text))
            //    {
            //        XNAMessageBox.Show(WindowManager, "错误", "我们无法推测这个任务包有几关，你需要指定一下");
            //        return;
            //    }

            //    if (!int.TryParse(_ctbMissionCount.Text, out missionCount))
            //    {
            //        XNAMessageBox.Show(WindowManager, "错误", "任务关数只能输入数字。");
            //        return;
            //    }
            //}
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
        //_ctbMissionPackID.Text = _pack.ID;
        _ctbMissionPackName.Text = _pack.Name;
        _ctbMissionPackDescription.Text = _pack.Name;
        _ctbMissionPackPath.Text = _pack.FilePath;
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

        _pack.ID = _pack.ID; //id
        _pack.Name = _ctbMissionPackName.Text.Trim(); //名称
        _pack.Description = _ctbMissionPackDescription.Text.Trim(); //介绍
        _pack.FilePath = _ctbMissionPackPath.Text.Trim();
        _pack.Author = _ctbAuthor.Text.Trim();
        _pack.LongDescription = _ctbMissionPackDescription.Text.Trim();
        _pack.Mod = _ctbCp.Text.Trim();
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
                if (k.ToUpper().Contains(_tbSearch.Text.ToUpper()) || v.ToUpper().Contains(_tbSearch.Text.ToUpper()))
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
        _menu.AddItem("修改这个条目", Edit, () => _mcListBoxCsfInfo.SelectedIndex > -1);
        _menu.AddItem("删除这个条目",Del, () => _mcListBoxCsfInfo.SelectedIndex > -1);

        
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