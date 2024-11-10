using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Rampastring.Tools;
using Localization;

namespace ClientCore
{
    /// <summary>
    /// Contains various static variables and constants that the client uses for operation.
    /// </summary>
    public static class ProgramConstants
    {
        public static readonly string StartupExecutable = Assembly.GetEntryAssembly().Location;

        public static readonly string StartupPath = SafePath.CombineDirectoryPath(new FileInfo(StartupExecutable).Directory.FullName);

        public const string MAIN_SERVER_URL = "https://dln1.yra2.com";

        public static string CUR_SERVER_URL = "";

        public static readonly string GamePath = SafePath.CombineDirectoryPath(SafePath.GetDirectory(StartupPath).Parent.Parent.FullName);

        //MOD游戏数据默认存放的MIX
        public static readonly string MOD_MIX = "EXPANDMD01.MIX";
        //皮肤文件存放的MIX
        public static readonly string SKIN_MIX = "RFSKIN.MIX";
        //任务文件存放的MIX
        public static readonly string MISSION_MIX = "MISSION.MIX";
        //存放YR或RA2资源的MIX
        public static readonly string CORE_MIX = "RFCORE.MIX";

        public static string ClientUserFilesPath => SafePath.CombineDirectoryPath(GamePath, "Client");

        public static event EventHandler PlayerNameChanged;

        public const string QRES_EXECUTABLE = "qres.dat";

        public const string CNCNET_PROTOCOL_REVISION = "R10";
        public const string LAN_PROTOCOL_REVISION = "RL7";
        //public const int LAN_PORT = 1234;
        //public const int LAN_INGAME_PORT = 1234;
        //public const int LAN_LOBBY_PORT = 1232;
        //public const int LAN_GAME_LOBBY_PORT = 1233;

        public const int LAN_PORT = 20386;
        public const int LAN_INGAME_PORT = 20386;
        public const int LAN_LOBBY_PORT = 20385;
        public const int LAN_GAME_LOBBY_PORT = 20384;

        public const char LAN_DATA_SEPARATOR = (char)01;
        public const char LAN_MESSAGE_SEPARATOR = (char)02;

        public const string SPAWNMAP_INI = "spawnmap.ini";
        public const string SPAWNER_SETTINGS = "spawn.ini";
        public const string SAVED_GAME_SPAWN_INI = "Saved Games/spawnSG.ini";

        public const int GAME_ID_MAX_LENGTH = 4;

        public static readonly Encoding LAN_ENCODING = Encoding.UTF8;

        public static string GAME_VERSION = "Undefined";

        private static string PlayerName = "No name";

        public static string PLAYERNAME
        {
            get { return PlayerName; }
            set
            {
                string oldPlayerName = PlayerName;
                PlayerName = value;
                if (oldPlayerName != PlayerName)
                    PlayerNameChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static string BASE_RESOURCE_PATH = "Resources";
        public static string RESOURCES_DIR = BASE_RESOURCE_PATH;

        public static int LOG_LEVEL = 1;

        public static bool IsInGame { get; set; }

        public static string GetResourcePath()
        {
            return SafePath.CombineDirectoryPath(GamePath, RESOURCES_DIR);
        }

        public static string GetBaseResourcePath()
        {
            return SafePath.CombineDirectoryPath(GamePath, BASE_RESOURCE_PATH);
        }

        public const string GAME_INVITE_CTCP_COMMAND = "INVITE";
        public const string GAME_INVITATION_FAILED_CTCP_COMMAND = "INVITATION_FAILED";
        public const string MAP_DOWNLOAD_NOTICE = "MAP_DOWNLOAD_NOTICE";
        public const string MAP_DOWNLOAD = "MAP_DOWNLOAD";

        public static string GetAILevelName(int aiLevel)
        {
            if (aiLevel > -1 && aiLevel < AI_PLAYER_NAMES.Count)
                return AI_PLAYER_NAMES[aiLevel];

            return "";
        }

        public static readonly List<string> TEAMS = new List<string> { "A", "B", "C", "D" };

        // Static fields might be initialized before the translation file is loaded. Change to readonly properties here.
        public static List<string> AI_PLAYER_NAMES => new List<string> { "Easy AI".L10N("UI:Main:EasyAIName"), "Medium AI".L10N("UI:Main:MediumAIName"), "Hard AI".L10N("UI:Main:HardAIName") };

        public static string LogFileName { get; set; }

        /// <summary>
        /// Gets or sets the action to perform to notify the user of an error.
        /// </summary>
        public static Action<string, string, bool> DisplayErrorAction { get; set; } = (title, error, exit) =>
        {
            Logger.Log(FormattableString.Invariant($"{(title is null ? null : title + Environment.NewLine + Environment.NewLine)}{error}"));
            ProcessLauncher.StartShellProcess(LogFileName);

            if (exit)
                Environment.Exit(1);
        };
    }
}