using System;
using System.Collections.Generic;
using ClientCore;
using Rampastring.Tools;

namespace Ra2Client.Domain.Multiplayer
{
    /// <summary>
    /// A multiplayer game mode.
    /// </summary>
    public class GameMode
    {
        public GameMode(string name)
        {
            Name = name;
            Initialize();
        }

        private const string BASE_INI_PATH = "INI/Multi/MapCode/";
        private const string SPAWN_INI_OPTIONS_SECTION = "ForcedSpawnIniOptions";

        /// <summary>
        /// ID.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 名称.
        /// </summary>
        public string UIName { get; private set; }

        /// <summary>
        /// 是否只能联机玩
        /// </summary>
        public bool MultiplayerOnly { get; private set; }

        /// <summary>
        /// 是否只能和人类玩
        /// </summary>
        public bool HumanPlayersOnly { get; private set; }

        /// <summary>
        /// 如果设置，玩家将被迫在此游戏模式中随机开始位置。
        /// </summary>
        public bool ForceRandomStartLocations { get; private set; }

        /// <summary>
        /// 如果设置了，玩家将被迫在此游戏模式中加入不同的队伍.
        /// </summary>
        public bool ForceNoTeams { get; private set; }

        public bool Special { get; private set; } = false;
        /// <summary>
        /// 不能选择的国家索引.
        /// </summary>
        public List<int> DisallowedPlayerSides = new List<int>();
        /// </summary>
        /// 在这个游戏模式中，任何地图都需要覆盖最少数量的玩家.
        /// </summary>
        public int MinPlayersOverride { get; private set; } = -1;
        /// <summary>
        /// 注入的INI
        /// </summary>
        private string mapCodeININame;

        /// <summary>
        /// 介绍
        /// </summary>
        public string modeText;

        private string forcedOptionsSection;

        public static readonly string ANNOTATION = "" +
            "# 在这里添加游戏模式。\r\n" +
            "# [GameMode ID]\r\n" +
            "# UIName = 名称。默认 ID \r\n" +
            "# MultiplayerOnly = 是否只能联机玩。默认 false \r\n" +
            "# HumanPlayersOnly = 是否只能和人类玩。默认 false \r\n" +
            "# mapCodeININame = 注入的INI。默认无 \r\n" +
            "# modeText = 游戏模式介绍。默认 UIName \r\n" +
            "# ForceNoTeams = 是否强制不同队伍。默认 false \r\n" +
            "# ForceRandomStartLocations = 是否强制随机位置。默认 false \r\n" +
            "# DisallowedPlayerSides = 不能选择的国家索引。默认 空"
            ;

        public List<Map> Maps = [];

        public List<KeyValuePair<string, bool>> ForcedCheckBoxValues = new List<KeyValuePair<string, bool>>();
        public List<KeyValuePair<string, int>> ForcedDropDownValues = new List<KeyValuePair<string, int>>();

        private List<KeyValuePair<string, string>> ForcedSpawnIniOptions = new List<KeyValuePair<string, string>>();

        public int CoopDifficultyLevel { get; set; }

        public void Initialize()
        {
            IniFile forcedOptionsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.GameModesIniPath));

            CoopDifficultyLevel = forcedOptionsIni.GetIntValue(Name, "CoopDifficultyLevel", 0);
            UIName = forcedOptionsIni.GetStringValue(Name, "UIName", Name);
            Special = forcedOptionsIni.GetBooleanValue(Name, "Special", false);
            MultiplayerOnly = forcedOptionsIni.GetBooleanValue(Name, "MultiplayerOnly", false);
            HumanPlayersOnly = forcedOptionsIni.GetBooleanValue(Name, "HumanPlayersOnly", false);
            ForceRandomStartLocations = forcedOptionsIni.GetBooleanValue(Name, "ForceRandomStartLocations", false);
            ForceNoTeams = forcedOptionsIni.GetBooleanValue(Name, "ForceNoTeams", false);
            MinPlayersOverride = forcedOptionsIni.GetIntValue(Name, "MinPlayersOverride", -1);
            forcedOptionsSection = forcedOptionsIni.GetStringValue(Name, "ForcedOptions", string.Empty);
            mapCodeININame = forcedOptionsIni.GetStringValue(Name, "MapCodeININame", Name + ".ini");
            modeText = forcedOptionsIni.GetStringValue(Name, "Text", UIName);

            string[] disallowedSides = forcedOptionsIni
                .GetStringValue(Name, "DisallowedPlayerSides", string.Empty)
                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string sideIndex in disallowedSides)
                DisallowedPlayerSides.Add(int.Parse(sideIndex));

            ParseForcedOptions(forcedOptionsIni);

            ParseSpawnIniOptions(forcedOptionsIni);
        }

        private void ParseForcedOptions(IniFile forcedOptionsIni)
        {
            if (string.IsNullOrEmpty(forcedOptionsSection))
                return;

            List<string> keys = forcedOptionsIni.GetSectionKeys(forcedOptionsSection);

            if (keys == null)
                return;

            foreach (string key in keys)
            {
                string value = forcedOptionsIni.GetStringValue(forcedOptionsSection, key, string.Empty);

                int intValue = 0;
                if (int.TryParse(value, out intValue))
                {
                    ForcedDropDownValues.Add(new KeyValuePair<string, int>(key, intValue));
                }
                else
                {
                    ForcedCheckBoxValues.Add(new KeyValuePair<string, bool>(key, Conversions.BooleanFromString(value, false)));
                }
            }
        }

        private void ParseSpawnIniOptions(IniFile forcedOptionsIni)
        {
            string section = forcedOptionsIni.GetStringValue(Name, "ForcedSpawnIniOptions", Name + SPAWN_INI_OPTIONS_SECTION);

            List<string> spawnIniKeys = forcedOptionsIni.GetSectionKeys(section);

            if (spawnIniKeys == null)
                return;

            foreach (string key in spawnIniKeys)
            {
                ForcedSpawnIniOptions.Add(new KeyValuePair<string, string>(key,
                    forcedOptionsIni.GetStringValue(section, key, string.Empty)));
            }
        }

        public void ApplySpawnIniCode(IniFile spawnIni)
        {
            foreach (KeyValuePair<string, string> key in ForcedSpawnIniOptions)
                spawnIni.SetValue("Settings", key.Key, key.Value);
        }

        public IniFile GetMapRulesIniFile()
        {
            return new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, BASE_INI_PATH, mapCodeININame));
        }

        protected bool Equals(GameMode other) => string.Equals(Name, other?.Name, StringComparison.InvariantCultureIgnoreCase);

        public override int GetHashCode() => (Name != null ? Name.GetHashCode() : 0);
    }
}
