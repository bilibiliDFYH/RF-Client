using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Rampastring.Tools;


namespace DTAConfig.Entity;

public class AI : InfoBaseClass
{
    /// <summary>
    /// 把AI信息写入配置文件
    /// </summary>
    /// <param name="AI">AI</param>
    public override void Create()
    {
        

        var iniFile = new IniFile(FileName ?? $"Mod&AI/Mod&AI{ID}.ini");
     
        iniFile.SetValue("AI", ID, ID);
        iniFile.AddSection(ID);
        iniFile.SetValue(ID, "MuVisible", MuVisible);
        iniFile.SetValue(ID, "Name", Name);
        iniFile.SetValue(ID, "Description", Description);
        iniFile.SetValue(ID, "FilePath", FilePath);
        iniFile.SetValue(ID, "Compatible", Compatible);
        iniFile.SetValue(ID, "Version", Version);
        iniFile.SetValue(ID, "Author", Author);
        iniFile.SetValue(ID, "Extension", Extension);
        iniFile.SetValue(ID, "ExtensionOn", ExtensionOn);

        iniFile.WriteIniFile();
    }

    public override Dictionary<string, string> GetProperties()
    {
        Dictionary<string, string> properties = new()
        {
            { "注册名", ID },
            { "名称", Name },
            { "介绍", Description },
            { "文件路径", FilePath },
            { "遭遇战是否可用", MuVisible.ToString() },
            { "兼容哪些AI", Compatible },
            { "版本", Version },
            { "作者", Author },
            { "可用的扩展", Extension },
            { "是否必须使用扩展", ExtensionOn.ToString() },
            { "是否为尤复AI", YR.ToString() },
            { "注册于", FileName }
        };

        return properties;
    }

    public static List<AI> AIs = new();
    /// <summary>
    /// 查询ID是否存在
    /// </summary>
    /// <param name="id">需要查询的ID</param>
    /// <returns>是否存在</returns>
    public static bool QueryID(string id)
        => AIs.Find(m => m.ID == id) != null;

    public static void reLoad()
    {

        AIs.Clear();

        
        var ModAI = Directory.GetFiles("Mod&AI/", "Mod&AI*.ini");

        foreach (var file in ModAI)
        {

            var ini = new IniFile(file);
            if (!ini.SectionExists("AI"))
                continue;
            foreach (var key in ini.GetSectionKeys("AI"))
            {
                var AIID = ini.GetValue("AI", key, string.Empty);
                var ai = new AI();
                ai.FilePath = ini.GetValue(key, "FilePath", $"Mod&AI/AI/{AIID}");
                ai.MuVisible = ini.GetValue(key, "MuVisible", true);
                if (ini.KeyExists(AIID, "Compatible"))
                {
                    ai.Compatible = ini.GetValue(AIID, "Compatible", string.Empty);
                    if (!string.IsNullOrEmpty(ai.Compatible))
                    {
                        foreach (var item in ai.Compatible.Split(','))
                        {
                            if (CompatibleDictionary.ContainsKey(item))
                            {
                                CompatibleDictionary[item] += ',' + AIID;
                            }
                            else
                                CompatibleDictionary.Add(item, AIID);
                        }
                    }
                }
                if (ini.KeyExists(key, "Extension"))
                {
                    ai.Extension = ini.GetValue(key, "Extension", string.Empty);
                    ai.ExtensionOn = ini.GetValue(key, "ExtensionOn", false);
                }
                ai.YR = ini.GetValue(key, "YR", true);
                ai = Init(ini, AIID, ai) as AI;
                if(ai.MuVisible)
                    AIs.Add(ai);
            }
        }
    }


    /// <summary>
    /// 兼容哪些AI
    /// </summary>
    public string Compatible { get; set; }

    /// <summary>
    /// 遭遇战是否显示
    /// </summary>
    public bool MuVisible { get; set; } = true;
    /// <summary>
    /// 注入的INI
    /// </summary>
    public string INI { get; set; }
    public bool YR { get; set; }
    /// <summary>
    /// 是否必须使用扩展
    /// </summary>
    public bool ExtensionOn { get; set; } = false;
    /// <summary>
    /// 使用的扩展列表
    /// </summary>
    public string Extension { get; set; } = "Ares,Phobos";// 使用的扩展列表
    /// <summary>
    /// 重写ToString方法
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"AI名称：{Name}\n" +
               $"注册名：{ID}\n" +
               $"介绍：{Description}\n" +
               $"文件路径：{FilePath}\n" +
               $"遭遇战是否可见：{(MuVisible ? "是" : "否")}\n" +
               $"版本：{Version}\n" +
               $"作者：{Author}\n" +
               $"可兼容的AI：{Compatible}\n" +
               $"注册于：{FileName}";
    }


    public static Dictionary<string, string> CompatibleDictionary = [];

    public static string GetCompatibleMods(string name) =>
    CompatibleDictionary.TryGetValue(name, out var value)
        ? name + "," + string.Join(",", value.Split(',').Select(GetCompatibleMods))
        : name;

    public static readonly string ANNOTATION = "" +
        "# 这里可以注册AI.\r\n" +
        "# [AI ID]" +
        "# Name = 名称。\r\n" +
        "# ID = 注册名。\r\n" +
        "# Description = 介绍。\r\n" +
        "# FilePath = 在重聚中的路径。\r\n" +
        "# Compatible = 兼容哪些AI。\r\n" +
        "# Version = 版本。\r\n" +
        "# FileName = 注册于哪个文件。\r\n" +
        "# Author = 作者。\r\n" +
        "# CanDel = 能否删除。";
}
