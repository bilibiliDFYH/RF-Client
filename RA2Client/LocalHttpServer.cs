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
using Localization.Tools;
using DTAConfig.OptionPanels;
using DTAConfig.Entity;
using SharpDX.Direct3D9;

namespace Ra2Client
{
    public static class LocalHttpServer
    {
        private static HttpListener listener;
        private static Thread listenerThread;
        public static int Port { get; private set; } = -1;
        public static bool IsRunning => listener != null && listener.IsListening;

        private static Dictionary<string, string> _installedMapIds = [];

        private static XNAMessageBox messageBox;

        public static void Start(WindowManager wm, int startPort = 27123, int maxTries = 10)
        {
            if (IsRunning) return;

            int tryPort = startPort;
            Exception lastEx = null;
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
                                HandleRequest(wm, context).ConfigureAwait(false);
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

        private static async Task HandleRequest(WindowManager wm, HttpListenerContext context)
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
                #region 下载地图
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
                    await 写入地图(map);

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
                #endregion
            }
            else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/downloadMissionPack")
            {
                #region 下载任务包
                try
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    string requestBody = await reader.ReadToEndAsync();

                    var missionPackVo = JsonSerializer.Deserialize<MissionPackVo>(requestBody);

                    写入任务包(missionPackVo);

                    var result = new
                    {
                        code = "200",
                    };
                    response.StatusCode = 200;
                    string jsonResult = JsonSerializer.Serialize(result);
                    byte[] buffer = Encoding.UTF8.GetBytes(jsonResult);
                    response.ContentType = "application/json";
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    #endregion
                }
                catch(Exception ex)
                {
                    Logger.Log(ex.ToString());
                    Console.WriteLine(ex.ToString());
                }
            }
            else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/mapExists")
            {
                try
                {
                    var mapId = request.QueryString["id"];
                    int status = 0; // 未下载

                    if (_installedMapIds.Keys.Contains(mapId))
                    {
                        if (_installedMapIds[mapId] != request.QueryString["updateTime"])
                        {
                            if (_installedMapIds[mapId] == string.Empty)
                                status = 1; //
                            else
                            {
                                status = 2; // 地图需要更新
                            }
                        }
                        else
                        {
                            status = 1;
                        }
                    }

                    var result = new
                    {
                        code = "200",
                        status,
                    };

                    string jsonResult = JsonSerializer.Serialize(result);
                    byte[] buffer = Encoding.UTF8.GetBytes(jsonResult);
                    response.ContentType = "application/json";
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.StatusCode = 200;

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
            else if(request.HttpMethod == "GET" && request.Url.AbsolutePath == "/missionPackExists")
            {
                var missionPackID = request.QueryString["id"];
                int status = 0; // 未下载

                var missionPack = MissionPack.MissionPacks.Find(m => m.ID == missionPackID);
                if(missionPack != null)
                {
                    if(missionPack.UpdateTime == request.QueryString["updateTime"])
                    {
                        status = 1; // 已安装
                    }
                    else
                    {
                        status = 2; // 需要更新
                    }
                }
        
                var result = new
                {
                    code = "200",
                    status,
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
                response.StatusCode = 404;
                response.Close();
            }
        }

        private static async Task 写入地图(Maps map)
        {
            if (map.file.StartsWith('u'))
            {
                string imageUrl = Path.Combine(NetWorkINISettings.Address, map.file).Replace("\\", "/");
                string imageSavePath = await NetWorkINISettings.DownloadImageAsync(imageUrl, "Maps/Multi/MapLibrary/", $"{map.id}.map");
            }
            else
            {
                Directory.CreateDirectory(ProgramConstants.MAP_PATH);
                File.WriteAllText(Path.Combine(ProgramConstants.MAP_PATH, $"{map.id}.map"), map.file);
            }

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


            mapIni.SetValue(sectionName, "MaxPlayers", map.maxPlayers);
            mapIni.SetValue(sectionName, "Description", $"[{map.maxPlayers}]{map.name}");
            mapIni.SetValue(sectionName, "GameModes", "常规作战,地图库");
            mapIni.SetValue(sectionName, "Author", map.author);
            mapIni.SetValue(sectionName, "Briefing", map.description);
            mapIni.SetValue(sectionName, "UpdateTime", map.updateTime ?? "");
            try
            {
                if (!string.IsNullOrEmpty(map.csf))
                {
                    string baseDir = Path.Combine("Maps", "Multi", "MapLibrary", map.id.ToString());

                    // 如果目录存在，删除整个目录及内容（慎用，确认安全）
                    if (Directory.Exists(baseDir))
                    {
                        Directory.Delete(baseDir, recursive: true);
                    }

                    // 重新创建目录
                    Directory.CreateDirectory(baseDir);

                    //byte[] fileBytes = Convert.FromBase64String(map.csf);

                    //string filePath = Path.Combine(baseDir, "ra2md.csf");

                    //File.WriteAllBytes(filePath, fileBytes);


                    // 路径使用正斜杠，符合配置格式
                    string relativePath = $"Maps/Multi/MapLibrary/{map.id}";
                    mapIni.SetValue(sectionName, "Mission", relativePath);
                    string csfURL = Path.Combine(NetWorkINISettings.Address, map.csf).Replace("\\", "/");
                    string imageSavePath = await NetWorkINISettings.DownloadImageAsync(csfURL, relativePath, "ra2md.csf");
                }
                else
                {
                    Console.WriteLine("map.csf为空或null，跳过写文件");
                }
            }
            catch (FormatException fe)
            {
                Console.WriteLine("Base64格式错误: " + fe.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("写文件时出现异常: " + ex.Message);
            }


            WriteListToIni(mapIni, sectionName, "Rule", map.rules);
            WriteListToIni(mapIni, sectionName, "EnemyHouse", map.enemyHouse);
            WriteListToIni(mapIni, sectionName, "AllyHouse", map.allyHouse);

            if (!string.IsNullOrEmpty(map.enemyHouse + map.allyHouse))
                mapIni.SetValue(sectionName, "IsCoopMission", true);

            mapIni.WriteIniFile();
        }

        private static async Task 写入任务包(MissionPackVo missionPackVo)
        {
            try
            {
                var fileName = Path.GetFileName(missionPackVo.file);
                string tmpFile = Path.Combine(ProgramConstants.GamePath, "tmp", fileName);
                string extractDir = Path.Combine(ProgramConstants.GamePath, "tmp", "MissionPack");

                string downloadUrl = Path.Combine(NetWorkINISettings.Address, missionPackVo.file);

                // 等待下载完成
                bool success = await NetWorkINISettings.DownloadFileAsync(downloadUrl, tmpFile);

                if (!success)
                {
                    Console.WriteLine($"❌ 下载任务包失败: {downloadUrl}");
                    return;
                }

                // 解压文件
                SevenZip.ExtractWith7Zip(tmpFile, extractDir, needDel:true);

                var missionPack = new MissionPack()
                {
                     ID = missionPackVo.id,
                     Name = missionPackVo.name,
                    LongDescription = missionPackVo.description,
                    UpdateTime = missionPackVo.updateTime
                };

                // 导入Mod
                ModManager.导入具体任务包(
                    true,
                    true,
                    Path.Combine(ProgramConstants.GamePath, "tmp","MissionPack"), 
                    muVisible: false,
                    m: missionPack
                );

                UserINISettings.Instance.重新加载地图和任务包?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 写入任务包时发生异常: {ex}");
            }
        }
        /// <summary>
        /// 将字符串用";"分隔后写入 INI
        /// </summary>
        private static void WriteListToIni(IniFile ini, string section, string keyPrefix, string data)
        {
            var list = data?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            for (int i = 0; i < list.Length; i++)
            {
                ini.SetValue(section, $"{keyPrefix}{i}", list[i]);
            }
        }

        public static void RefreshInstalledMapIds()
        {
            if (!Directory.Exists(ProgramConstants.MAP_PATH))
            {
                _installedMapIds.Clear();
                return;
            }

            var ini = new IniFile(Path.Combine("Maps\\Multi\\MPMapsMapLibrary.ini"));

            _installedMapIds = Directory.GetFiles(ProgramConstants.MAP_PATH, "*.map")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Select(idStr => idStr)
                .Where(id => id != "-1")
                .ToDictionary(
                    id => id,
                    id => ini.GetValue(id.ToString(), "updateTime", string.Empty)
                );
        }

    }

}