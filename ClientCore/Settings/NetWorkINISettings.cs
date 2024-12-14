using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClientCore.Enums;
using DTAConfig.Entity;
using Rampastring.Tools;

namespace ClientCore.Settings;

public class NetWorkINISettings
{
    private static NetWorkINISettings _instance;

    private const string locServerPath = "Resources/ServerList";                                              // 本地服务器列表路径
    private static string remoteFileUrl = "";                                                                 // 远程设置路径
    private const string localFilePath = "Resources\\Settings";                                               // 本地设置路径
    private const string secUpdater = "Updater";                                                              //更新段                                                    //组件段

#if DEBUG
   private const string Address = "https://api.yra2.com/";
  // private const string Address = "http://localhost:9088/";
#else
    private const string Address = "https://api.yra2.com/";
#endif
    
    public static event EventHandler DownloadCompleted;

    public IniFile SettingsIni { get; private set; }

    public List<ServerMirror> UpdaterServers { get; private set; } = null;         //服务器更新列表

    protected NetWorkINISettings(IniFile iniFile)
    {
        SettingsIni = iniFile;

        //if (SettingsIni.SectionExists(secMain))
        //{
        //    Announcement = SettingsIni.GetStringValue(secMain, "Announcement", string.Empty);
        //}

        if (SettingsIni.SectionExists(secUpdater))
        {
            string strServers = SettingsIni.GetStringValue(secUpdater, "Servers", "");
            if(!string.IsNullOrEmpty(strServers))
            {
                string[] serverGroup = strServers.Split(',');
                UpdaterServers = [];
                for (int i = 0; i < serverGroup.Length; i++)
                {
                    var us = new ServerMirror()
                    {
                        Type = SettingsIni.GetIntValue(serverGroup[i], "Type", 0),
                        Name = SettingsIni.GetStringValue(serverGroup[i], "Name", $"服务器#{i}"),
                        Location = SettingsIni.GetStringValue(serverGroup[i], "Location", "Unkown"),
                        URL = SettingsIni.GetStringValue(serverGroup[i], "Url", ""),
                    };
                    UpdaterServers.Add(us);
                }
            }
        }
    }

    public static NetWorkINISettings Instance
    {
        get => _instance;
    }

    public static async Task Initialize()
    {
        var remoteServerUrl = (await Get<string>("dict/GetValue?section=dln&key=main_address")).Item1 ?? "https://autopatch1-js.yra2.com/Client/ServerList";

        if (!DownloadSettingFile(remoteServerUrl, locServerPath))
            Logger.Log("Request Server List File Failed!");

        var IniServer = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, locServerPath));
        foreach (var secName in IniServer.GetSections())
        {
            string strID = IniServer.GetStringValue(secName, "Id", "");
            string strName = IniServer.GetStringValue(secName, "Name", "");
            string strURL = IniServer.GetStringValue(secName, "Url", "");
            if (!string.IsNullOrEmpty(strURL))
            {
                remoteFileUrl = strURL + "/Client/Public/Settings";
                if (DownloadSettingFile(remoteFileUrl, localFilePath))
                {
                    ProgramConstants.CUR_SERVER_URL = strURL;
                    Console.WriteLine("Activated Server：{0}",strURL);
                    Logger.Log($"Requset Server:{strID} {strName} {strURL} Successed");
                    break;
                }
                else
                    Logger.Log($"Requset Server:{strID} {strName} {strURL} Failed");
            }
        }

        //如果远程获取文件失败则读取本地配置
        if (!string.IsNullOrEmpty(remoteFileUrl) || File.Exists(localFilePath)) 
        {
            var iniFile = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, localFilePath));
            _instance = new NetWorkINISettings(iniFile);
            Updater.Initialize(
                ClientConfiguration.Instance.SettingsIniName,
                ClientConfiguration.Instance.LocalGame,
                SafePath.GetFile(ProgramConstants.StartupExecutable).Name);
            Updater.ServerMirrors = _instance.UpdaterServers;
            DownloadCompleted?.Invoke(null, EventArgs.Empty);
        }
    }

    protected static bool DownloadSettingFile(string strSerPath, string strLocPath)
    {
        try
        {
            return WebHelper.HttpDownFile(strSerPath, strLocPath);
        }
        catch (Exception ex)
        {
            Logger.Log("连接服务器出错。" + ex);
            return false;
        }
    }

    public void SetServerList()
    {
        UpdaterServers = Updater.ServerMirrors;
    }

    public static async Task<(T,string)> Post<T>(string url, object obj)
    {
        using var client = new HttpClient();

        // 将对象转换为 JSON 字符串
        string jsonContent = JsonSerializer.Serialize(obj);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", UserINISettings.Instance.Token.Value);

        // 发送 POST 请求并获取响应
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync($"{Address}{url}", new StringContent(jsonContent, Encoding.UTF8, "application/json")).ConfigureAwait(false);
        

        // 读取响应内容
        T responseData;

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var resResult = JsonSerializer.Deserialize<ResResult<T>>(responseContent);
            responseData = resResult.data;
            if(responseData == null 
                    ||responseData.Equals(default(T)))
            {
                return (default,resResult.message);
            }
        }
        else
        {
            return default;
        }

        // 返回响应数据
        return (responseData, string.Empty);

        }
        catch (Exception ex)
        {
            // 处理请求异常
            Console.WriteLine($"请求失败：{ex.Message}");
            return (default, ex.Message); ;
        }
    }

    public static async Task<(T, string)> Post<T>(string url, MultipartFormDataContent formData)
    {
        
        using var client = new HttpClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", UserINISettings.Instance.Token.Value);
        
        HttpResponseMessage response;
        try
        {

            // 发送 POST 请求并传递 formData
            response = await client.PostAsync($"{Address}{url}", formData).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            // 处理请求异常
            Console.WriteLine($"请求失败：{ex.Message}");
            return default;
        }

        // 读取响应内容
        T responseData;

        if (response.IsSuccessStatusCode)
        {
            //var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            //var responseContent = Encoding.UTF8.GetString(bytes);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            
            var resResult = JsonSerializer.Deserialize<ResResult<T>>(responseContent);
            responseData = resResult.data;
            if(responseData == null
                    || responseData.Equals(default(T)))
                return (default,resResult.message);
        }
        else
        {
            return default;
        }

        // 返回响应数据
        return (responseData, string.Empty);
    }

    public static async Task<(T,string)> Get<T>(string url)
    {
        try
        {
            using var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", UserINISettings.Instance.Token.Value);
            // 发送 GET 请求并获取响应
            HttpResponseMessage response;

            client.Timeout = new TimeSpan(10 * TimeSpan.TicksPerSecond);
            response = await client.GetAsync($"{Address}{url}").ConfigureAwait(false);
        
            // 读取响应内容
            T responseData = default;
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var resResult = JsonSerializer.Deserialize<ResResult<T>>(responseContent);
                responseData = resResult.data;
                if (responseData == null)
                    return (default, resResult.message);
            }
            else
            {
                return (default(T), "网络错误");
            }

            // 返回响应数据
            return (responseData,string.Empty);
        }
        catch (Exception ex)
        {
            // 处理请求异常
            Console.WriteLine($"请求失败：{ex.Message}");
            return default;
        }
    }
    public static async Task<(bool, string)> DownLoad(string url, string outputPath)
    {
        using var client = new HttpClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", UserINISettings.Instance.Token.Value);

        try
        {
            // 发送 GET 请求并获取响应
            var response = await client.GetAsync($"{Address}{url}").ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {

                // 读取响应流并将其保存为文件
                await using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                await using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    await contentStream.CopyToAsync(fileStream).ConfigureAwait(false);
                }

                // 返回文件下载成功的消息
                return (true, "文件下载成功");
            }
            else
            {
                return (false, "用户信息过期，请重新登录。  ");
            }
        }
        catch (HttpRequestException ex)
        {
            // 处理请求异常
            Console.WriteLine($"请求失败：{ex.Message}");
            return (false, ex.Message);
        }
        catch (Exception ex)
        {
            // 处理其他异常
            Console.WriteLine($"发生错误：{ex.Message}");
            return (false, ex.Message);
        }
    }


}