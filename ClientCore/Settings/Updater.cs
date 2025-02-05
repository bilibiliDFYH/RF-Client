namespace ClientCore.Settings;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Localization.Tools;
using Rampastring.Tools;

public static class Updater
{
    private const string SECOND_STAGE_UPDATER = "ClientUpdater.dll";

#if DEBUG
    public const string VERSION_FILE = "VersionDev";
#else
    public const string VERSION_FILE = "Version";
#endif

    /// <summary>
    /// 游戏根路径.
    /// </summary>
    public static string GamePath { get; set; } = string.Empty;

    /// <summary>
    /// 本地游戏资源路径.
    /// </summary>
    public static string ResourcePath { get; set; } = string.Empty;

    /// <summary>
    /// 更新器的本地游戏ID.
    /// </summary>
    public static string LocalGame { get; private set; } = "None";

    /// <summary>
    /// 更新器调用的可执行文件名称.
    /// </summary>
    public static string CallingExecutableFileName { get; private set; } = string.Empty;

    /// <summary>
    /// 更新服务器组（只读）
    /// </summary>
    public static List<ServerMirror> ServerMirrors { get => serverMirrors; set => serverMirrors = value; }

    /// <summary>
    /// Update server URL for current update mirror if available.
    /// </summary>
    public static string CurrentUpdateServerURL
        => serverMirrors is { Count: > 0 }
            ? serverMirrors[currentServerMirrorIndex].URL
            : null;

    private static VersionState _versionState = VersionState.UNKNOWN;

    /// <summary>
    /// Current version state of the updater.
    /// </summary>
    public static VersionState versionState
    {
        get => _versionState;

        private set
        {
            _versionState = value;
            DoOnVersionStateChanged();
        }
    }

    /// <summary>
    /// 如可用，当前更新是否需要手动下载？
    /// </summary>
    public static bool ManualUpdateRequired { get; private set; }

    /// <summary>
    /// 如可用，当前更新手动下载地址
    /// </summary>
    public static string ManualDownloadURL { get; private set; } = string.Empty;

    /// <summary>
    /// 本地更新器版本
    /// </summary>
    public static string UpdaterVersion { get; private set; } = "N/A";

    /// <summary>
    /// 本地游戏版本
    /// </summary>
    public static string GameVersion { get; set; } = "N/A";

    /// <summary>
    /// 服务器游戏版本
    /// </summary>
    public static string ServerGameVersion { get; private set; } = "N/A";

    /// <summary>
    /// 更新文件大小
    /// </summary>
    public static int UpdateSizeInKb { get; private set; }

    /// <summary>
    /// 更新文件时间
    /// </summary>
    public static string UpdateTime { get; private set; }


    // Misc.
    private static IniFile settingsINI;
    private static int currentServerMirrorIndex;
    private static List<ServerMirror> serverMirrors;

    public static VersionFileConfig serverVerCfg;
    public static VersionFileConfig clientVerCfg;
    // File infos.
    private static readonly List<UpdaterFileInfo> serverFileInfos = new();
    private static readonly List<UpdaterFileInfo> localFileInfos = new();

    private static readonly ProgressMessageHandler SharedProgressMessageHandler = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
        AutomaticDecompression = DecompressionMethods.All
    });

    private static readonly HttpClient SharedHttpClient = new(SharedProgressMessageHandler, true)
    {
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
    };

    // Current update / download related.
    private static bool terminateUpdate;
    private static string currentFilename;
    private static int currentFileSize;
    private static int totalDownloadedKbs;

    /// <summary>
    /// Initializes the updater.
    /// </summary>
    /// <param name="resourcePath">Path of the resource folder of client / game.</param>
    /// <param name="settingsIniName">Client settings INI filename.</param>
    /// <param name="localGame">Local game ID of the current game.</param>
    /// <param name="callingExecutableFileName">File name of the calling executable.</param>
    /// <param name="servers">Server list</param>
    public static void Initialize(string settingsIniName, string localGame, string callingExecutableFileName)
    {
        Logger.Log("更新: 初始化更新模块.");

        settingsINI = new(SafePath.CombineFilePath(GamePath, settingsIniName));
        LocalGame = localGame;
        CallingExecutableFileName = callingExecutableFileName;
    }

    /// <summary>
    /// Checks if there are available updates.
    /// </summary>
    public static void CheckForUpdates()
    {
        Logger.Log("更新: 检查更新.");
        if (versionState is not VersionState.UPDATECHECKINPROGRESS and not VersionState.UPDATEINPROGRESS)
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            DoVersionCheckAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    /// <summary>
    /// Checks version information of local files.
    /// </summary>
    public static void CheckLocalFileVersions()
    {
        Logger.Log("更新: 检查本地文件版本.");

        string strUpdaterFile = SafePath.CombineFilePath(ResourcePath, "Binaries", SECOND_STAGE_UPDATER);
        if(File.Exists(strUpdaterFile))
        {
            Assembly assembly = Assembly.LoadFile(strUpdaterFile);
            UpdaterVersion = assembly.GetName().Version.ToString();
        }

        localFileInfos.Clear();
        var version = new IniFile(SafePath.CombineFilePath(GamePath, VERSION_FILE));
        if (!File.Exists(version.FileName))
            return;

        clientVerCfg = new VersionFileConfig()
        {
            Version = version.GetStringValue("Client", "Version", string.Empty),
            UpdaterVersion = version.GetStringValue("Client", "UpdaterVersion", "N/A"),
        };

        var lstKeys = version.GetSectionKeys("");
        if (null != lstKeys && lstKeys.Count > 0)
        {
            foreach ( var strKey in lstKeys)
            {
                string[] strArray = version.GetStringValue("FileVerify", strKey, string.Empty).Split(',');
                var item = new UpdaterFileInfo(
                        SafePath.CombineFilePath(strKey), Conversions.IntFromString(strArray[1], 0))
                {
                    Identifier = strArray[0],
                    ArchiveIdentifier = "",
                    ArchiveSize = 0
                };
                localFileInfos.Add(item);
            }
        }

        OnLocalFileVersionsChecked?.Invoke();
    }

    /// <summary>
    /// Starts update process.
    /// </summary>
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    public static void StartUpdate() => PerformUpdateAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

    /// <summary>
    /// Stops current update process.
    /// </summary>
    public static void StopUpdate() => terminateUpdate = true;

    /// <summary>
    /// Clears current version file information.
    /// </summary>
    public static void ClearVersionInfo()
    {
        serverFileInfos.Clear();
        localFileInfos.Clear();
        GameVersion = "1.5.0.0";
        //versionState = VersionState.UNKNOWN;
    }

    /// <summary>
    /// Moves update mirror down in list of update mirrors.
    /// </summary>
    /// <param name="mirrorIndex">Index of mirror to move in the list.</param>
    public static void MoveMirrorDown(int nType, int mirrorIndex)
    {
        var lstServers = serverMirrors.Where(f => f.Type.Equals(nType)).ToList();
        if (mirrorIndex > lstServers.Count - 2 || mirrorIndex < 0)
            return;

        (lstServers[mirrorIndex], lstServers[mirrorIndex + 1]) = (lstServers[mirrorIndex + 1], lstServers[mirrorIndex]);
        var lstOtherServers = serverMirrors.Where(f => f.Type.Equals(Math.Abs(nType - 1))).ToList();
        serverMirrors.Clear();
        serverMirrors.AddRange(lstServers);
        serverMirrors.AddRange(lstOtherServers);
    }

    /// <summary>
    /// Moves update mirror up in list of update mirrors.
    /// </summary>
    /// <param name="mirrorIndex">Index of mirror to move in the list.</param>
    public static void MoveMirrorUp(int nType, int mirrorIndex)
    {
        var lstServers = serverMirrors.Where(f => f.Type.Equals(nType)).ToList();

        if (lstServers.Count <= mirrorIndex || mirrorIndex < 1)
            return;

        (lstServers[mirrorIndex], lstServers[mirrorIndex - 1]) = (lstServers[mirrorIndex - 1], lstServers[mirrorIndex]);
        var lstOtherServers = serverMirrors.Where(f => f.Type.Equals(Math.Abs(nType - 1))).ToList();
        serverMirrors.Clear();
        serverMirrors.AddRange(lstServers);
        serverMirrors.AddRange(lstOtherServers);
    }

    internal static void UpdateUserAgent(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.UserAgent.Clear();

        if (GameVersion != "N/A")
            httpClient.DefaultRequestHeaders.UserAgent.Add(new(LocalGame, GameVersion));

        if (UpdaterVersion != "N/A")
            httpClient.DefaultRequestHeaders.UserAgent.Add(new(nameof(Updater), UpdaterVersion));

        httpClient.DefaultRequestHeaders.UserAgent.Add(new("Client", Assembly.GetEntryAssembly().GetName().Version.ToString()));
    }

    /// <summary>
    /// Deletes file and waits until it has been deleted.
    /// </summary>
    /// <param name="filepath">File to delete.</param>
    /// <param name="timeout">Maximum time to wait in milliseconds.</param>
    internal static void DeleteFileAndWait(string filepath, int timeout = 10000)
    {
        FileInfo fileInfo = SafePath.GetFile(filepath);
        using var fw = new FileSystemWatcher(fileInfo.DirectoryName, fileInfo.Name);
        using var mre = new ManualResetEventSlim();

        fw.EnableRaisingEvents = true;
        fw.Deleted += (_, _) =>
        {
            mre.Set();
        };
        fileInfo.Delete();
        mre.Wait(timeout);
    }

    /// <summary>
    /// Creates all directories required for file path.
    /// </summary>
    /// <param name="filePath">File path.</param>
    internal static void CreatePath(string filePath)
    {
        FileInfo fileInfo = SafePath.GetFile(filePath);

        if (!fileInfo.Directory.Exists)
            fileInfo.Directory.Create();
    }

    internal static string GetUniqueIdForFile(string filePath)
    {
        using var md = MD5.Create();
        md.Initialize();
        using FileStream fs = SafePath.GetFile(GamePath, filePath).OpenRead();
        md.ComputeHash(fs);
        var builder = new StringBuilder();

        foreach (byte num2 in md.Hash)
            builder.Append(num2);

        md.Clear();
        return builder.ToString();
    }

    /// <summary>
    /// Checks if file
    /// </summary>
    public static bool IsFileNonexistantOrOriginal(string filePath)
    {
        if(null == localFileInfos || 0 == localFileInfos.Count)
            return true;

        var info = localFileInfos.Find(f => f.Filename.Equals(filePath, StringComparison.OrdinalIgnoreCase));
        if (info == null)
            return true;

        string uniqueIdForFile = GetUniqueIdForFile(info.Filename);
        return info.Identifier == uniqueIdForFile;
    }

    /// <summary>
    /// Performs a version file check on update server.
    /// </summary>
    private static void DoVersionCheckAsync()
    {
        Logger.Log("更新: 检查文件版本.");

        UpdateSizeInKb = 0;

        try
        {
            versionState = VersionState.UPDATECHECKINPROGRESS;

            if (ServerMirrors.Count == 0)
            {
                Logger.Log("更新：这不是合法的更新地址!");
            }
            else
            {
                Logger.Log("更新：检查更新服务.");

                UpdateUserAgent(SharedHttpClient);

                //FileInfo dirInfo = SafePath.GetFile(GamePath, "Tmp");
                //if(!Directory.Exists(dirInfo.FullName))
                //    Directory.CreateDirectory(dirInfo.FullName);
                //else
                //    Directory.Delete(dirInfo.FullName, true);

                //FileInfo downloadFile = SafePath.GetFile(dirInfo.FullName, FormattableString.Invariant($"{VERSION_FILE}"));
                //if (File.Exists(downloadFile.FullName))
                //    downloadFile.Delete();

                // 根据条件判断服务器类型
                //var serversCondi = ServerMirrors.Where(f => f.Type.Equals(UserINISettings.Instance.Beta.Value)).ToList();
                
                // 根据更新服务器顺序依次查找合适的服务器信息
                //while (currentServerMirrorIndex < serversCondi.Count)
                //{
                //    try
                //    {
                //        Logger.Log("更新：Trying to connect to update mirror " + serversCondi[currentServerMirrorIndex].URL);
                //        if (WebHelper.HttpDownFile(serversCondi[currentServerMirrorIndex].URL + VERSION_FILE, downloadFile.FullName))
                //            break;
                //    }
                //    catch (Exception e)
                //    {
                //        Logger.Log("更新：Error connecting to update mirror. Error message: " + e.Message);
                //        Logger.Log("更新：Seeking other mirrors...");

                //        if (currentServerMirrorIndex >= ServerMirrors.Count)
                //        {
                //            currentServerMirrorIndex = 0;
                //            throw new("Unable to connect to update servers.");
                //        }
                //    }
                //    currentServerMirrorIndex++;
                //}

                //Logger.Log("更新：下载版本信息.");
                //var version = new IniFile(downloadFile.FullName);

                var version = NetWorkINISettings.Get<ClientCore.Entity.Updater>($"updater/getLatestInfo?type={UserINISettings.Instance.Beta.Value}").GetAwaiter().GetResult().Item1 ?? throw new("Update server integrity error while checking for updates.");
                serverVerCfg = new VersionFileConfig()
                {
                    Version = version.version,
                    //UpdaterVersion = version.GetStringValue("Client", "UpdaterVersion", "N/A"),
                    //ManualDownURL = version.GetStringValue("Client", "ManualDownloadURL", string.Empty),
                    Package = version.file,
                    Hash = version.hash,
                    Size = (int)version.size,
                    Logs = version.log,
                    time = version.updateTime
                };

                Logger.Log("更新：Server game version is " + serverVerCfg.Version + ", local version is " + GameVersion);
                ServerGameVersion = serverVerCfg.Version;
                UpdateTime = serverVerCfg.time;
                if (!CheckHasNewVersion(ServerGameVersion, GameVersion))
                {
                    versionState = VersionState.UPTODATE;
                //    downloadFile.Delete();
                    DoFileIdentifiersUpdatedEvent();
                }
                else
                {
                    string strServUpdaterVer = serverVerCfg.UpdaterVersion;
                   
                    if (strServUpdaterVer != "N/A" && UpdaterVersion == "N/A" && strServUpdaterVer != UpdaterVersion)
                    {
                        Logger.Log("更新：Server updater  version is set to " + strServUpdaterVer + " and is different to local update system version " + UpdaterVersion + ". Manual update required.");
                        versionState = VersionState.OUTDATED;
                        ManualUpdateRequired = true;
                        ManualDownloadURL = serverVerCfg.ManualDownURL;
                     //   downloadFile.Delete();
                        DoFileIdentifiersUpdatedEvent();
                    }
                    else
                    {
                        UpdateSizeInKb = serverVerCfg.Size;
                        VersionCheckHandle();
                    }
                }
            }
        }
        catch (Exception exception)
        {
            versionState = VersionState.UNKNOWN;
            Logger.Log("更新：An error occured while performing version check: " + exception.Message);
            DoFileIdentifiersUpdatedEvent();
        }
    }

    private static bool CheckHasNewVersion(string strSer, string strLoc)
    {
        Version v1 = new Version(strSer);

        Version v2 = new Version(strLoc);

        if(v1.Major <= v2.Major)
        {
            if(v1.Minor <= v2.Minor)
            {
                if(v1.Build <= v2.Build)
                {
                    if (-1 != v1.Revision && -1 != v2.Revision)
                    {
                        if (v1.Revision <= v2.Revision)
                            return false;
                    }
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Executes after-update script file.
    /// </summary>
    private static async ValueTask ExecuteAfterUpdateScriptAsync()
    {
        Logger.Log("更新：Downloading updateexec.");
        try
        {
            var fileStream = new FileStream(SafePath.CombineFilePath(GamePath, "updateexec"), new FileStreamOptions
            {
                Access = FileAccess.Write,
                BufferSize = 0,
                Mode = FileMode.Create,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough,
                Share = FileShare.None
            });

            await using (fileStream.ConfigureAwait(false))
            {
                Stream stream = await SharedHttpClient.GetStreamAsync(ServerMirrors[currentServerMirrorIndex].URL + "updateexec").ConfigureAwait(false);

                await using (stream.ConfigureAwait(false))
                {
                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }
        }
        catch (Exception exception)
        {
            Logger.Log("更新：Warning: Downloading updateexec failed: " + exception.Message);
            return;
        }

        ExecuteScript("updateexec");
    }

    ///// <summary>
    ///// Executes pre-update script file.
    ///// </summary>
    ///// <returns>True if succesful, otherwise false.</returns>
    //private static async ValueTask<bool> ExecutePreUpdateScriptAsync()
    //{
    //    Logger.Log("更新：Downloading preupdateexec.");
    //    try
    //    {
    //        var fileStream = new FileStream(SafePath.CombineFilePath(GamePath, "preupdateexec"), new FileStreamOptions
    //        {
    //            Access = FileAccess.Write,
    //            BufferSize = 0,
    //            Mode = FileMode.Create,
    //            Options = FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough,
    //            Share = FileShare.None
    //        });

    //        await using (fileStream.ConfigureAwait(false))
    //        {
    //            Stream stream = await SharedHttpClient.GetStreamAsync(ServerMirrors[currentServerMirrorIndex].URL + "preupdateexec").ConfigureAwait(false);

    //            await using (stream.ConfigureAwait(false))
    //            {
    //                await stream.CopyToAsync(fileStream).ConfigureAwait(false);
    //            }
    //        }
    //    }
    //    catch (Exception exception)
    //    {
    //        Logger.Log("更新：Warning: Downloading preupdateexec failed: " + exception.Message);
    //        return false;
    //    }

    //    ExecuteScript("preupdateexec");
    //    return true;
    //}

    /// <summary>
    /// Executes a script file.
    /// </summary>
    /// <param name="fileName">Filename of the script file.</param>
    private static void ExecuteScript(string fileName)
    {
        Logger.Log("更新：Executing " + fileName + ".");
        FileInfo scriptFileInfo = SafePath.GetFile(GamePath, fileName);
        var script = new IniFile(scriptFileInfo.FullName);

        // Delete files.
        foreach (string key in GetKeys(script, "Delete"))
        {
            Logger.Log("更新：" + fileName + ": Deleting file " + key);

            try
            {
                SafePath.DeleteFileIfExists(GamePath, key);
            }
            catch (Exception ex)
            {
                Logger.Log("更新：" + fileName + ": Deleting file " + key + "failed: " + ex.Message);
            }
        }

        // Rename files.
        foreach (string key in GetKeys(script, "Rename"))
        {
            string newFilename = SafePath.CombineFilePath(script.GetStringValue("Rename", key, string.Empty));
            if (string.IsNullOrWhiteSpace(newFilename))
                continue;
            try
            {
                Logger.Log("更新：" + fileName + ": Renaming file '" + key + "' to '" + newFilename + "'");

                FileInfo file = SafePath.GetFile(GamePath, key);

                if (file.Exists)
                    file.MoveTo(SafePath.CombineFilePath(GamePath, newFilename));
            }
            catch (Exception ex)
            {
                Logger.Log("更新：" + fileName + ": Renaming file '" + key + "' to '" + newFilename + "' failed: " + ex.Message);
            }
        }

        // Rename folders.
        foreach (string key in GetKeys(script, "RenameFolder"))
        {
            string newDirectoryName = script.GetStringValue("RenameFolder", key, string.Empty);
            if (string.IsNullOrWhiteSpace(newDirectoryName))
                continue;
            try
            {
                Logger.Log("更新：" + fileName + ": Renaming directory '" + key + "' to '" + newDirectoryName + "'");

                DirectoryInfo directory = SafePath.GetDirectory(GamePath, key);

                if (directory.Exists)
                    directory.MoveTo(SafePath.CombineDirectoryPath(GamePath, newDirectoryName));
            }
            catch (Exception ex)
            {
                Logger.Log("更新：" + fileName + ": Renaming directory '" + key + "' to '" + newDirectoryName + "' failed: " + ex.Message);
            }
        }

        // Rename & merge files / folders.
        foreach (string key in GetKeys(script, "RenameAndMerge"))
        {
            string directoryName = key;
            string directoryNameToMergeInto = script.GetStringValue("RenameAndMerge", key, string.Empty);
            if (string.IsNullOrWhiteSpace(directoryNameToMergeInto))
                continue;
            try
            {
                Logger.Log("更新：" + fileName + ": Merging directory '" + directoryName + "' with '" + directoryNameToMergeInto + "'");
                DirectoryInfo directoryToMergeInto = SafePath.GetDirectory(GamePath, directoryNameToMergeInto);
                DirectoryInfo gameDirectory = SafePath.GetDirectory(GamePath, directoryName);

                if (!gameDirectory.Exists)
                    continue;

                if (!directoryToMergeInto.Exists)
                {
                    Logger.Log("更新：" + fileName + ": Destination directory '" + directoryNameToMergeInto + "' does not exist, renaming.");
                    gameDirectory.MoveTo(directoryToMergeInto.FullName);
                }
                else
                {
                    Logger.Log("更新：" + fileName + ": Destination directory '" + directoryNameToMergeInto + "' exists, performing selective merging.");
                    FileInfo[] files = gameDirectory.GetFiles();
                    foreach (FileInfo file in files)
                    {
                        FileInfo fileToMergeInto = SafePath.GetFile(directoryToMergeInto.FullName, file.Name);
                        if (fileToMergeInto.Exists)
                        {
                            Logger.Log("更新：" + fileName + ": Destination file '" + directoryNameToMergeInto + "/" + file.Name +
                                "' exists, removing original source file " + directoryName + "/" + file.Name);
                            fileToMergeInto.Delete();
                        }
                        else
                        {
                            Logger.Log("更新：" + fileName + ": Destination file '" + directoryNameToMergeInto + "/" + file.Name +
                                "' does not exist, moving original source file " + directoryName + "/" + file.Name);
                            file.MoveTo(fileToMergeInto.FullName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("更新：" + fileName + ": Merging directory '" + directoryName + "' with '" + directoryNameToMergeInto + "' failed: " + ex.Message);
            }
        }

        // Delete folders.
        foreach (string sectionName in new string[] { "DeleteFolder", "ForceDeleteFolder" })
        {
            foreach (string key in GetKeys(script, sectionName))
            {
                try
                {
                    Logger.Log("更新：" + fileName + ": Deleting directory '" + key + "'");

                    SafePath.DeleteDirectoryIfExists(true, GamePath, key);
                }
                catch (Exception ex)
                {
                    Logger.Log("更新：" + fileName + ": Deleting directory '" + key + "' failed: " + ex.Message);
                }
            }
        }

        // Delete folders, if empty.
        foreach (string key in GetKeys(script, "DeleteFolderIfEmpty"))
        {
            try
            {
                Logger.Log("更新：" + fileName + ": Deleting directory '" + key + "' if it's empty.");

                DirectoryInfo directoryInfo = SafePath.GetDirectory(GamePath, key);

                if (directoryInfo.Exists)
                {
                    if (!directoryInfo.EnumerateFiles().Any())
                    {
                        directoryInfo.Delete();
                    }
                    else
                    {
                        Logger.Log("更新：" + fileName + ": Directory '" + key + "' is not empty!");
                    }
                }
                else
                {
                    Logger.Log("更新：" + fileName + ": Specified directory does not exist.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("更新：" + fileName + ": Deleting directory '" + key + "' if it's empty failed: " + ex.Message);
            }
        }

        // Create folders.
        foreach (string key in GetKeys(script, "CreateFolder"))
        {
            try
            {
                DirectoryInfo directoryInfo = SafePath.GetDirectory(GamePath, key);
                if (!directoryInfo.Exists)
                {
                    Logger.Log("更新：" + fileName + ": Creating directory '" + key + "'");
                    directoryInfo.Create();
                }
                else
                {
                    Logger.Log("更新：" + fileName + ": Directory '" + key + "' already exists.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("更新：" + fileName + ": Creating directory '" + key + "' failed: " + ex.Message);
            }
        }

        scriptFileInfo.Delete();
    }

    /// <summary>
    /// Handle version check.
    /// </summary>
    private static void VersionCheckHandle()
    {
        Logger.Log("更新：Gathering file to be downloaded. Server file is: " + serverVerCfg.Package);
        versionState = VersionState.OUTDATED;
        ManualUpdateRequired = false;
        DoFileIdentifiersUpdatedEvent();
    }

    /// <summary>
    /// Downloads files required for update and starts second-stage updater.
    /// </summary>
    private static async Task PerformUpdateAsync()
    {
        Logger.Log("更新：Starting update.");
        versionState = VersionState.UPDATEINPROGRESS;

        try
        {
            UpdateUserAgent(SharedHttpClient);

            SharedProgressMessageHandler.HttpReceiveProgress += ProgressMessageHandlerOnHttpReceiveProgress;

            //if (!await ExecutePreUpdateScriptAsync().ConfigureAwait(false))
            //    throw new("Executing preupdateexec failed.");

            VersionCheckHandle();

            if (string.IsNullOrEmpty(ServerGameVersion) || ServerGameVersion == "N/A" || versionState != VersionState.OUTDATED)
                throw new("Update server integrity error.");

            versionState = VersionState.UPDATEINPROGRESS;

            totalDownloadedKbs = currentFileSize = 0;

            if (terminateUpdate)
            {
                Logger.Log("更新：Terminating update because of user request.");
                versionState = VersionState.OUTDATED;
                ManualUpdateRequired = false;
                terminateUpdate = false;
            }
            else
            {

                int num = 0;
                if (terminateUpdate)
                {
                    Logger.Log("更新：Terminating update because of user request.");
                    versionState = VersionState.OUTDATED;
                    ManualUpdateRequired = false;
                    terminateUpdate = false;
                    return;
                }

                while (true)
                {
                    currentFilename = serverVerCfg.Package;
                    currentFileSize += serverVerCfg.Size;
                    bool flag = await DownloadFileAsync(currentFilename).ConfigureAwait(false);

                    if (terminateUpdate)
                    {
                        Logger.Log("更新：Terminating update because of user request.");
                        versionState = VersionState.OUTDATED;
                        ManualUpdateRequired = false;
                        terminateUpdate = false;
                        return;
                    }

                    if (flag)
                    {
                        totalDownloadedKbs += currentFileSize;
                        break;
                    }

                    num++;
                    if (num == 2)
                    {
                        Logger.Log("更新：Too many retries for downloading file " + currentFilename + ". Update halted.");
                        throw new("Too many retries for downloading file " + currentFilename);
                    }
                }

                if (terminateUpdate)
                {
                    Logger.Log("更新：Terminating update because of user request.");
                    versionState = VersionState.OUTDATED;
                    ManualUpdateRequired = false;
                    terminateUpdate = false;
                }
                else
                {
                    DirectoryInfo tmpDirInfo = SafePath.GetDirectory(GamePath, "Tmp");

                    try
                    {
                        //判断文件是否合法，远程未设置Hash则不判断
                        var pkgFile = SafePath.CombineFilePath(tmpDirInfo.FullName, currentFilename);
                        if (!string.IsNullOrEmpty(serverVerCfg.Hash))
                        {
                            string strFileHash = Utilities.CalculateSHA1ForFile(pkgFile);
                            if (serverVerCfg.Hash != strFileHash)
                            {
                                Logger.Log("更新：Terminating update because of file hash is incorrect.");
                                versionState = VersionState.OUTDATED;
                                ManualUpdateRequired = false;
                                DoOnUpdateFailed("更新包校验不通过");
                                return;
                            }
                        }
                        //解压更新包文件
                        //bool bRet = SevenZip.CompressWith7Zip(pkgFile, tmpDirInfo.FullName);
                        //if (bRet)
                        //    File.Delete(pkgFile);

                        SevenZip.ExtractWith7Zip(pkgFile, tmpDirInfo.FullName,needDel:true);
                    }
                    catch(Exception ex)
                    {
                        Logger.Log(ex.ToString());
                    }

                    tmpDirInfo.Refresh();
                    if (tmpDirInfo.Exists)
                    {
                        //判断ClientUpdater是否有更新，有则优先移动到安装目录\Resources\Binaries\目录下等待更新
                        DirectoryInfo curClientUpdaterDir = SafePath.GetDirectory(tmpDirInfo.FullName, "Resources", "Binaries");
                        FileInfo curClientUpdater = SafePath.GetFile(curClientUpdaterDir.FullName, SECOND_STAGE_UPDATER);
                        Logger.Log("更新：Checking & moving second-stage updater files.");

                        FileInfo clientUpdaterFile = SafePath.GetFile(ResourcePath, "Binaries", SECOND_STAGE_UPDATER);

                        //移动文件到游戏目录下(文件会占用导致失败？)
                        //if (curClientUpdater.Exists)
                        {
                            //try
                            //{

                            //    Logger.Log("更新：Moving second-stage updater file " + curClientUpdater.Name + ".");
                            //    curClientUpdater.MoveTo(clientUpdaterFile.FullName, true);

                            //}
                            //catch(Exception ex)
                            //{
                            //    File.Move(curClientUpdater.FullName, clientUpdaterFile.FullName, true);
                            //}
                            //versionState = VersionState.OUTDATED;
                            //ManualUpdateRequired = true;
                            //return;
                        }

                        //启动游戏目录下的更新器
                        Logger.Log("更新：Launching second-stage updater executable " + clientUpdaterFile.FullName + ".");

                        string strDotnet = @"C:\Program Files (x86)\dotnet\dotnet.exe";
                        if (Environment.Is64BitProcess)
                            strDotnet = @"C:\Program Files\dotnet\dotnet.exe";

                        if (!File.Exists(strDotnet))
                        {
                            Logger.Log("dotnet not exits.");
                            DoOnUpdateFailed("dotnet.exe不存在");
                            return;
                        }

                        using var _ = Process.Start(new ProcessStartInfo
                        {
                            FileName = strDotnet,
                            Arguments = "\"" + clientUpdaterFile.FullName + "\" " + CallingExecutableFileName + " \"" + GamePath + "\"",
                            UseShellExecute = true
                        });

                        Logger.Log("\"" + clientUpdaterFile.FullName + "\" " + CallingExecutableFileName + " \"" + GamePath + "\"");

                        Environment.Exit(0);
                        //Restart?.Invoke(null, EventArgs.Empty);
                    }
                    else
                    {
                        Logger.Log("更新：Update completed successfully.");
                        totalDownloadedKbs = 0;
                        UpdateSizeInKb = 0;
                        CheckLocalFileVersions();
                        ServerGameVersion = "N/A";
                        versionState = VersionState.UPTODATE;
                        DoUpdateCompleted();

                        //if (AreCustomComponentsOutdated())
                        //    DoCustomComponentsOutdatedEvent();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log("更新：An error occurred during the update. message: " + ex.Message);
            versionState = VersionState.UNKNOWN;
            DoOnUpdateFailed(ex.Message);
        }
        finally
        {
            SharedProgressMessageHandler.HttpReceiveProgress -= ProgressMessageHandlerOnHttpReceiveProgress;
        }
    }

    /// <summary>
    /// Downloads and handles individual file.
    /// </summary>
    /// <param name="fileInfo">File info for the file.</param>
    /// <returns>True if successful, otherwise false.</returns>
    private static async ValueTask<bool> DownloadFileAsync(string strfile)
    {
        Logger.Log("更新：Initiliazing download of file " + strfile);

        UpdateDownloadProgress(0);

        string prefixPath = "Tmp";
        FileInfo locFile = SafePath.GetFile(GamePath, prefixPath, strfile);

        try
        {
            int currentServerMirrorId = Updater.currentServerMirrorIndex;
            
            var serversCondi = ServerMirrors.Where(f => f.Type.Equals(UserINISettings.Instance.Beta.Value)).ToList();
            var serverFile = (serversCondi[currentServerMirrorId].URL + strfile).Replace(@"\", "/", StringComparison.OrdinalIgnoreCase);
            CreatePath(locFile.FullName);

            Logger.Log("更新：Downloading file " + strfile);
            var fileStream = new FileStream(locFile.FullName, new FileStreamOptions
            {
                Access = FileAccess.Write,
                BufferSize = 0,
                Mode = FileMode.Create,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough,
                Share = FileShare.None
            });

            await using (fileStream.ConfigureAwait(false))
            {
                Stream stream = await SharedHttpClient.GetStreamAsync(new Uri(serverFile)).ConfigureAwait(false);

                await using (stream.ConfigureAwait(false))
                {
                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }

            OnFileDownloadCompleted?.Invoke(strfile);
            Logger.Log("更新：Download of file " + strfile + " finished - verifying.");

            //    if (fileInfo.Archived)
            //    {
            //        Logger.Log("更新：File is an archive.");
            //        string archiveIdentifier = CheckFileIdentifiers(strfile, fileRelativePath, fileInfo.ArchiveIdentifier);

            //        if (string.IsNullOrEmpty(archiveIdentifier))
            //        {
            //            Logger.Log("更新：Archive " + strfile + extraExtension + " is intact. Unpacking...");
            //            ZIP.Unpack(downloadFile.FullName, decompressedFile.FullName);
            //            downloadFile.Delete();
            //        }
            //        else
            //        {
            //            Logger.Log("更新：Downloaded archive " + strfile + extraExtension + " has a non-matching identifier: " + archiveIdentifier + " against " + fileInfo.ArchiveIdentifier);
            //            DeleteFileAndWait(downloadFile.FullName);

            //            return false;
            //        }
            //    }

            //string fileIdentifier = CheckFileIdentifiers(strfile, SafePath.CombineFilePath(prefixPath, strfile), fileInfo.Identifier);
            //if (string.IsNullOrEmpty(fileIdentifier))
            //{
            //    Logger.Log("更新：File " + strfile + " is intact.");

            //    return true;
            //}

            //Logger.Log("更新：Downloaded file " + strfile + " has a non-matching identifier: " + fileIdentifier + " against " + fileInfo.Identifier);
            //DeleteFileAndWait(decompressedFile.FullName);
        }
        catch (Exception exception)
        {
            Logger.Log("更新：An error occurred while downloading file " + strfile + ": " + exception.Message);
            return false;
        }
        return true;
    }

    /// <summary>
    /// 更新下载进度
    /// </summary>
    /// <param name="progressPercentage">Progress percentage.</param>
    private static void UpdateDownloadProgress(int progressPercentage)
    {
        double num = currentFileSize * (progressPercentage / 100.0);
        double num2 = totalDownloadedKbs + num;

        int totalPercentage = 0;

        if (UpdateSizeInKb is > 0 and < int.MaxValue)
            totalPercentage = (int)(num2 / UpdateSizeInKb * 100.0);

        DownloadProgressChanged(currentFilename, progressPercentage, totalPercentage);
    }

    /// <summary>
    /// Gets keys from INI file section.
    /// </summary>
    /// <param name="iniFile">INI file.</param>
    /// <param name="sectionName">Section name.</param>
    /// <returns>List of keys or empty list if section does not exist or no keys were found.</returns>
    private static List<string> GetKeys(IniFile iniFile, string sectionName)
    {
        List<string> keys = iniFile.GetSectionKeys(sectionName);

        if (keys != null)
            return keys;

        return new();
    }

    /// <summary>
    /// Attempts to get file identifier for a file.
    /// </summary>
    /// <param name="filePath">File path of file.</param>
    /// <returns>File identifier if successful, otherwise empty string.</returns>
    private static string TryGetUniqueId(string filePath)
    {
        try
        {
            return GetUniqueIdForFile(filePath);
        }
        catch
        {
            return string.Empty;
        }
    }

    public static event NoParamEventHandler FileIdentifiersUpdated;

    public static event LocalFileCheckProgressChangedCallback LocalFileCheckProgressChanged;

    public static event NoParamEventHandler OnCustomComponentsOutdated;

    public static event NoParamEventHandler OnLocalFileVersionsChecked;

    public static event NoParamEventHandler OnUpdateCompleted;

    public static event SetExceptionCallback OnUpdateFailed;

    public static event NoParamEventHandler OnVersionStateChanged;

    public static event FileDownloadCompletedEventHandler OnFileDownloadCompleted;

    public static event EventHandler Restart;

    public static event UpdateProgressChangedCallback UpdateProgressChanged;

    public delegate void LocalFileCheckProgressChangedCallback(int checkedFileCount, int totalFileCount);

    public delegate void NoParamEventHandler();

    public delegate void SetExceptionCallback(string strMsg);

    public delegate void UpdateProgressChangedCallback(string currFileName, int currFilePercentage, int totalPercentage);

    public delegate void FileDownloadCompletedEventHandler(string archiveName);

    private static void ProgressMessageHandlerOnHttpReceiveProgress(object sender, HttpProgressEventArgs e) => UpdateDownloadProgress(e.ProgressPercentage);

    private static void DownloadProgressChanged(string currFileName, int currentFilePercentage, int totalPercentage) => UpdateProgressChanged?.Invoke(currFileName, currentFilePercentage, totalPercentage);

    private static void DoCustomComponentsOutdatedEvent() => OnCustomComponentsOutdated?.Invoke();

    private static void DoFileIdentifiersUpdatedEvent()
    {
        Logger.Log("更新：File identifiers updated.");
        FileIdentifiersUpdated?.Invoke();
    }

    private static void DoOnUpdateFailed(string strMsg) => OnUpdateFailed?.Invoke(strMsg);

    private static void DoOnVersionStateChanged() => OnVersionStateChanged?.Invoke();

    private static void DoUpdateCompleted() => OnUpdateCompleted?.Invoke();
}

public readonly record struct ServerMirror(int Type, string Name, string Location, string URL);

public readonly record struct VersionFileConfig(string Version, string UpdaterVersion, string ManualDownURL, string Package, string Hash, int Size, string Logs,string time);

public enum VersionState
{
    UNKNOWN,                //未知
    UPTODATE,               //最新版
    OUTDATED,               //已过期
    MISMATCHED,             //不匹配
    UPDATEINPROGRESS,       //更新中
    UPDATECHECKINPROGRESS,  //检查更新中
}

internal sealed record UpdaterFileInfo(string Filename, int Size)
{
    public string Identifier { get; set; }

    public string ArchiveIdentifier { get; set; }

    public int ArchiveSize { get; set; }

    public bool Archived => !string.IsNullOrEmpty(ArchiveIdentifier) && ArchiveSize > 0;
}