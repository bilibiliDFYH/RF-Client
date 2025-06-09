using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Ra2Client.Domain.Multiplayer;
using ClientCore.Entity;
using System.Text.Json;
using ClientCore;
using Rampastring.Tools;
using ClientCore.Settings;
using ClientGUI;
using Rampastring.XNAUI;

namespace Ra2Client
{
    public static class LocalHttpServer
    {
        private static HttpListener? listener;
        private static Thread? listenerThread;
        public static int Port { get; private set; } = -1;
        public static bool IsRunning => listener != null && listener.IsListening;

        private static HashSet<int> _installedMapIds = new();

        private static XNAMessageBox messageBox;

        public static void Start(WindowManager wm,int startPort = 27123, int maxTries = 10)
        {
            if (IsRunning) return;

            int tryPort = startPort;
            Exception? lastEx = null;
            RefreshInstalledMapIds();
           
            for (int i = 0; i < maxTries; i++)
            {
                try
                {
                    Port = tryPort;
                    string prefix = $"http://localhost:{Port}/";

                    listener = new HttpListener();
                    listener.Prefixes.Add(prefix);
                    listener.Start();

                    Console.WriteLine($"✅ 本地服务启动成功：{prefix}");

                    listenerThread = new Thread(() =>
                    {
                        while (listener.IsListening)
                        {
                            try
                            {
                                var context = listener.GetContext();
                                HandleRequest(wm,context);
                            }
                            catch (HttpListenerException)
                            {
                                break;
                            }
                        }
                    });

                    listenerThread.Start();

                    return; // 启动成功，退出方法
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    tryPort++;
                }
            }

            throw new Exception($"无法启动本地服务，尝试了{maxTries}个端口，最后错误：{lastEx}");
        }


        public static void Stop()
        {
            if (!IsRunning) return;

            listener!.Stop();
            listenerThread?.Join();
            listener = null;
            listenerThread = null;
            Port = -1;

            Console.WriteLine("🛑 本地服务已停止");
        }

        private static async Task HandleRequest(WindowManager wm,HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            // ===== CORS 设置 =====
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "*");

            // ===== OPTIONS 预检请求 =====
            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            // ===== 下载地图处理逻辑 =====
            if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/downloadMap")
            {
                try
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    string requestBody = await reader.ReadToEndAsync();

                    var map = JsonSerializer.Deserialize<Maps>(requestBody);

                    if (map == null)
                    {
                        Console.WriteLine("❌ 解析地图对象失败");
                        response.StatusCode = 400;
                        return;
                    }

                   // Console.WriteLine($"✅ 收到地图下载请求：{map.name} ({map.id})");

                    // 1. 写入 map 文件
                    Directory.CreateDirectory(ProgramConstants.MAP_PATH);
                    File.WriteAllText(Path.Combine(ProgramConstants.MAP_PATH, $"{map.id}.map"), map.file);

                    // 2. 下载图片
                    if (map.img != null)
                    {
                        string imageUrl = Path.Combine(NetWorkINISettings.Address, map.img).Replace("\\", "/");
                        string imageSavePath = await NetWorkINISettings.DownloadImageAsync(imageUrl, "Maps/Multi/MapLibrary/", $"{map.id}.jpg");
                    }
                    //if (imageSavePath == null)
                    //{
                    //    Console.WriteLine("❌ 图片下载失败");
                    //    response.StatusCode = 500;
                    //    return;
                    //}

                    // 3. 写入 INI 配置
                    var mapIni = new IniFile("Maps\\Multi\\MPMapsMapLibrary.ini");
                    string sectionName = $"Maps/Multi/MapLibrary/{map.id}";

                    mapIni.SetValue(sectionName, "Description", $"[{map.maxPlayers}]{map.name}");
                    mapIni.SetValue(sectionName, "GameModes", "常规作战,地图库");
                    mapIni.SetValue(sectionName, "Author", map.author);
                    mapIni.SetValue(sectionName, "Briefing", map.description);

                    WriteListToIni(mapIni, sectionName, "Rule", map.rules);
                    WriteListToIni(mapIni, sectionName, "EnemyHouse", map.enemyHouse);
                    WriteListToIni(mapIni, sectionName, "AllyHouse", map.allyHouse);

                    mapIni.WriteIniFile();

                    response.StatusCode = 200;
                    RefreshInstalledMapIds();

                    messageBox?.Disable();
                    messageBox?.Dispose();
                    messageBox?.Detach();
                    
                    messageBox = new XNAMessageBox(wm, "新增地图", "检测到新地图，是否刷新地图列表？", XNAMessageBoxButtons.YesNo);
                    messageBox.YesClickedAction += (_) => { UserINISettings.Instance.重新加载地图和任务包?.Invoke(); };
                    messageBox.Show();

                    var result = new
                    {
                        code = "200",
                    };
                    string jsonResult = JsonSerializer.Serialize(result);
                    byte[] buffer = Encoding.UTF8.GetBytes(jsonResult);
                    response.ContentType = "application/json";
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine("❌ JSON解析错误：" + ex.Message);
                    response.StatusCode = 400;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ 处理地图下载请求时发生错误：" + ex.Message);
                    response.StatusCode = 500;
                }
                finally
                {
                    response.ContentType = "application/json";
                    response.Close(); // 一定要关闭响应
                }
            }
            else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/mapExists")
            {
                try
                {
                    if (int.TryParse(request.QueryString["id"], out int mapId))
                    {
                        bool exists = _installedMapIds.Contains(mapId);
                        var result = new
                        {
                            code = "200",
                            data = exists,
                        };

                        string jsonResult = JsonSerializer.Serialize(result);
                        byte[] buffer = Encoding.UTF8.GetBytes(jsonResult);
                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        response.StatusCode = 200;
                    }
                    else
                    {
                        Console.WriteLine("❌ 无效的地图ID");
                        response.StatusCode = 400;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("检查地图是否存在时出错：" + ex.Message);
                    response.StatusCode = 500;
                }
                finally
                {
                    response.Close();
                }
            }
            else
            {
                response.StatusCode = 404;
                response.Close();
            }
        }

        /// <summary>
        /// 将字符串用 ; 分隔后写入 INI
        /// </summary>
        private static void WriteListToIni(IniFile ini, string section, string keyPrefix, string? data)
        {
            var list = data?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            for (int i = 0; i < list.Length; i++)
            {
                ini.SetValue(section, $"{keyPrefix}{i + 1}", list[i]);
            }
        }

        private static void RefreshInstalledMapIds()
        {
            if (!Directory.Exists(ProgramConstants.MAP_PATH))
            {
                _installedMapIds.Clear();
                return;
            }
            _installedMapIds = Directory.GetFiles(ProgramConstants.MAP_PATH, "*.map")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Select(idStr => int.TryParse(idStr, out var id) ? id : -1)
                .Where(id => id != -1)
                .ToHashSet();
        }

    }

}
