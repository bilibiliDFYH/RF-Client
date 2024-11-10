using ClientCore;
using Rampastring.Tools;
using SharpDX.MediaFoundation.DirectX;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DTAConfig
{
    public static class RenderImage
    {
        public static int RenderCount = 0;
        public static event EventHandler RenderCompleted;

        public static async Task<bool> RenderOneImageAsync(string mapPath)
        {
            try
            {
                string mapName = Path.GetFileNameWithoutExtension(mapPath);
                string inputPath = Path.Combine(Path.GetDirectoryName(mapPath), $"thumb_{mapName}.png");
                string outputPath = Path.Combine(Path.GetDirectoryName(mapPath), $"{mapName}.png");
                string strCmdText = $"-i \"{ProgramConstants.GamePath}{mapPath}\" -o \"{mapName}\" -m \"{ProgramConstants.GamePath}\\\" -Y -z +(1280,768) --thumb-png --bkp ";
               //  Console.WriteLine(strCmdText);
                Process process = new Process();
                process.StartInfo.FileName = $"{ProgramConstants.GamePath}Resources\\RandomMapGenerator_RA2\\Map Renderer\\CNCMaps.Renderer.exe";
                process.StartInfo.Arguments = strCmdText;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                // 异步执行渲染单张图片的逻辑
                await Task.Run(() =>
                {
                    
                    process.Start();
                    process.WaitForExit();
                    process.Close();

                    if (File.Exists(inputPath))
                    {
                        try
                        {
                            File.Move(inputPath, outputPath, true);
                        }
                        catch {
                           
                        }
                    }
                    
                    // 渲染成功，增加计数并触发事件
                    
                    Interlocked.Increment(ref RenderCount);
                    RenderCompleted?.Invoke(null, EventArgs.Empty);
                });

                if (File.Exists(outputPath))
                    return true;
                return false;
            }
            catch (Exception ex)
            {
                // 渲染出错，记录异常并触发事件
                Logger.Log($"Error rendering image {mapPath}: {ex.Message}");
                Interlocked.Increment(ref RenderCount);
                RenderCompleted?.Invoke(null, EventArgs.Empty);
                return false;
            }
        }

        // 渲染多张图片的方法
        public static async Task RenderImagesAsync(string[] mapPaths)
        {
            RenderCount = 0;
            int maxDegreeOfParallelism = 5; // 设置最大并发数量，可以根据系统性能和需求进行调整
            List<Task> tasks = [];

            // 使用 SemaphoreSlim 控制并发数量
            using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            foreach (string mapPath in mapPaths)
            {
                // 等待可用的执行“槽位”
                await semaphore.WaitAsync();

                // 使用 Task.Run 创建任务并控制并发量
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await RenderOneImageAsync(mapPath);
                    }
                    finally
                    {
                        // 完成任务后释放“槽位”
                        semaphore.Release();
                    }
                });

                tasks.Add(task);
            }

            // 等待所有任务完成
            await Task.WhenAll(tasks);
        }



    }
}
