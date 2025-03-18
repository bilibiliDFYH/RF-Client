using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Rampastring.Tools;
using Localization;
using Rampastring.XNAUI;

namespace ClientCore
{
    /// <summary>
    /// Contains various static variables and constants that the client uses for operation.
    /// </summary>
    public static class ProgramConstants
    {
        public static readonly string StartupExecutable = Assembly.GetEntryAssembly().Location;

        public static readonly string StartupPath = SafePath.CombineDirectoryPath(new FileInfo(StartupExecutable).Directory.FullName);

        public static readonly string 游戏目录 = Path.Combine(SafePath.CombineDirectoryPath(SafePath.GetDirectory(StartupPath).Parent.Parent.FullName), "Run");

        public static readonly List<int> Version_24H2 = [26100, 26120];

        public static string CUR_SERVER_URL = "";

        public static bool SkipLogo = false;

        public static readonly string GamePath = SafePath.CombineDirectoryPath(SafePath.GetDirectory(StartupPath).Parent.Parent.FullName);

        //MOD游戏数据默认存放的MIX
        public static readonly string MOD_MIX = "EXPANDMD01.MIX";
        //皮肤文件存放的MIX
        public static readonly string SKIN_MIX = "RFSKIN.MIX";
        //任务文件存放的MIX
        public static readonly string MISSION_MIX = "MISSION.MIX";
        //存放YR或RA2资源的MIX
        public static readonly string CORE_MIX = "RFCORE.MIX";

        public static readonly string ARES = "Ares3";
        public static readonly string PHOBOS = "Phobos40";

        public static string ClientUserFilesPath => SafePath.CombineDirectoryPath(GamePath, "Client");

        public static event EventHandler PlayerNameChanged;

        public const string QRES_EXECUTABLE = "qres.dat";

        public const string CNCNET_PROTOCOL_REVISION = "R12";
        public const string LAN_PROTOCOL_REVISION = "RL7";

        public const int LAN_PORT = 22233;
        public const int LAN_INGAME_PORT = 22233;
        public const int LAN_LOBBY_PORT = 22231;
        public const int LAN_GAME_LOBBY_PORT = 22232;

        public const char LAN_DATA_SEPARATOR = (char)01;
        public const char LAN_MESSAGE_SEPARATOR = (char)02;

        public const string SPAWNMAP_INI = "spawnmap.ini";
        public const string SPAWNER_SETTINGS = "spawn.ini";
        public const string SAVED_GAME_SPAWN_INI = "Saved Games/spawnSG.ini";

        public const int GAME_ID_MAX_LENGTH = 4;

        public static readonly Encoding LAN_ENCODING = Encoding.UTF8;

        public static string GAME_VERSION = "Undefined";

        private static string PlayerName = "No name";

        public static readonly Dictionary<string, string> PureHashes = new()
    {
        { "expandmd01.mix", "a596d0acbf25aeeed5115cd2818e2fbd8d90f248" },
        { "gamemd.exe", "189a5a868b3cef8d3d1a58ac3cf0a5241675e4ea" },
        { "langmd.mix", "6c87bbc21a33e5cd6db5834798562189ed827963" },
        { "language.mix", "ea35ef0a88b9334b60c9ab4cff619752fcf06f68" },
        { "MAPSMD03.MIX", "c5106432628a576e30348289cf7beb663b1931cd" },
        { "movmd03.mix", "bb95f17d9243e483e268617dbce738cf49527ccf" },
        { "MULTIMD.MIX", "9ad3b25bc95daef55dd63e8d6c6b4a815a775c4d" },
        { "ra2.mix", "3bd92246320f4bf1ff1ed76207ee793c33ff6a05" },
        { "ra2md.mix", "091bd7f219836a330b2339e3f8606954b4a9b01f" },
        { "Blowfish.dll","214000ba48040818b4f0d7ff06c4debbb1ae2274" },
        { "BINKW32.DLL","613f81f82e12131e86ae60dd318941f40db2200f" }
    };

        public static void 清理游戏目录()
        {
            foreach (var file in Directory.GetFiles(游戏目录))
            {
                if (!PureHashes.ContainsKey(Path.GetFileName(file)))
                {
                    File.Delete(file);
                }
            }
        }

        public static bool 判断目录是否为纯净尤复(string path)
        {
            if (!Directory.Exists(path))
                return false;

            foreach (var fileName in PureHashes.Keys)
            {
                if (!File.Exists(Path.Combine(path, fileName)))
                    return false;
            }

            foreach (var fileName in Directory.GetFiles(path))
            {
                var name = Path.GetFileName(fileName);

                if (PureHashes.ContainsKey(name))
                {
                    var fileHash = Utilities.CalculateSHA1ForFile(fileName);
                    if (fileHash != PureHashes[name])
                        return false;
                }
            }

            return true;
        }

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

        public static bool 清理缓存()
        {
            try
            {
                FileHelper.ForceDeleteDirectory(游戏目录);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
                return false;
            }

        }


    }
}