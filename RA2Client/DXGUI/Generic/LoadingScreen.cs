using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.Settings;
using ClientGUI;
using Ra2Client.Domain.Multiplayer;
using Ra2Client.DXGUI.Multiplayer.CnCNet;
using Ra2Client.Online;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input;
using DTAConfig.Entity;
using NAudio.Wave;
using Sdcb.FFmpeg.Utils;

namespace Ra2Client.DXGUI.Generic
{
    public class LoadingScreen : XNAWindow
    {
        public LoadingScreen(
            CnCNetManager cncnetManager,
            WindowManager windowManager,
            IServiceProvider serviceProvider,
            MapLoader mapLoader
        ) : base(windowManager)
        {
            this.cncnetManager = cncnetManager;
            this.serviceProvider = serviceProvider;
            this.mapLoader = mapLoader;
          
        }

        private static readonly object locker = new object();

        private MapLoader mapLoader;

        private bool visibleSpriteCursor;

        private Task updaterInitTask;
        private Task mapLoadTask;
        private readonly CnCNetManager cncnetManager;
        private readonly IServiceProvider serviceProvider;

        private int time;
        private int index = 1;

        private Song themeSongLoad;
        private AudioFileReader audioFile;
        private WaveOutEvent outputDevice;

        public override void Initialize()
        {
            
            ClientRectangle = new Rectangle(0, 0, 1280, 768);
            Name = "LoadingScreen";

            //if (ProgramConstants.跳过Logo)
            //{
            //    index = 123;
               // BackgroundTexture = (AssetLoader.LoadTextureUncached("Dynamicbg/loadingscreen/loading.jpg"));
            //}

            if (!UserINISettings.Instance.video_wallpaper || ProgramConstants.跳过Logo)
            {
               
                string path = $"Resources/{UserINISettings.Instance.ClientTheme}Wallpaper";
                if (!Directory.Exists(path))
                    path = $"Resources/Wallpaper";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
               
                string[] Wallpaper = Directory.GetFiles(path);

                if (UserINISettings.Instance.Random_wallpaper)
                {
                    var random = new Random();
                    int i = random.Next(0, Wallpaper.Length);
                    BackgroundTexture = AssetLoader.LoadTexture(Wallpaper[i]);
                }
               
                else if (Wallpaper.Length > 0)
                {
                    BackgroundTexture = AssetLoader.LoadTexture(Wallpaper[0]);
                }

              mapLoadTask = mapLoader.LoadMapsAsync();
            }
            else
            {
                //themeSongLoad = new Song("Resources/themeSongLoad.wma");
                PlayLoadMusic();
            }
         
            base.Initialize();

            CenterOnParent();

            bool initUpdater = !ClientConfiguration.Instance.ModMode;
           
            if (initUpdater)
            {
                updaterInitTask = new Task(InitUpdater);
                updaterInitTask.Start();
            }

           // UserINISettings.Instance.WindowTitleProcess = $"已加载地图{1}个";

            if (Cursor.Visible)
            {
                Cursor.Visible = false;
                visibleSpriteCursor = true;
            }

            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;

            LeftClick += (_, _) => { if(index < 122) index = 122; };
        }
        private void PlayLoadMusic()
        {
            //if (!isMediaPlayerAvailable)
            //    return; // SharpDX fails at music playback on Vista
            try
            {
                audioFile = new AudioFileReader($"{ProgramConstants.GamePath}Resources/LoadMusicTheme.wma");
                outputDevice = new WaveOutEvent();

                outputDevice.Init(audioFile);
                outputDevice.Play();
            }
            catch (Exception ex)
            {
                Logger.Log("播放音频失败! " + ex.Message);
            }
            
        }


        private void InitUpdater()
        {
            Updater.OnLocalFileVersionsChecked += LogGameClientVersion;
            Updater.CheckLocalFileVersions();
        }

        private void LogGameClientVersion()
        {
            Logger.Log($"客户端版本: {ClientConfiguration.Instance.LocalGame} {Updater.GameVersion}");
            Updater.OnLocalFileVersionsChecked -= LogGameClientVersion;
        }

        bool isFinish = false;

        private void Finish()
        {
            if (isFinish) return;
            isFinish = true;
            ProgramConstants.GAME_VERSION = ClientConfiguration.Instance.ModMode ?
                "N/A" : Updater.GameVersion;
         
            MainMenu mainMenu = serviceProvider.GetService<MainMenu>();
            
            WindowManager.AddAndInitializeControl(mainMenu);
         
            mainMenu.PostInit();
           
            if (UserINISettings.Instance.AutomaticCnCNetLogin &&
                NameValidator.IsNameValid(ProgramConstants.PLAYERNAME) == null)
            {
                cncnetManager.Connect();
            }
            
            if (UserINISettings.Instance.video_wallpaper)
                UnloadContent();

            WindowManager.RemoveControl(this);
            Cursor.Visible = visibleSpriteCursor;

        }

        private void Keyboard_OnKeyPressed(object sender, Rampastring.XNAUI.Input.KeyPressEventArgs e)
        {
            var keyPressed = e.PressedKey;

            if (keyPressed == Keys.Escape && index < 122)
            {
                index = 122;
            }
        }


        public override void Update(GameTime gameTime)
        {
            if (UserINISettings.Instance.video_wallpaper && !ProgramConstants.跳过Logo && Directory.Exists("Resources/Dynamicbg"))
            {
                if (time >= (index > 39 ? 3 : 5) && index < 123)//播放速度
                {
                    time = 0;
                    BackgroundTexture?.Dispose();
                    if (index < 122)
                    {
                        
                        BackgroundTexture = (AssetLoader.LoadTextureUncached("Dynamicbg/loadingscreen/eawwlogo" + index + ".jpg"));
                        index++;
                    }
                    else
                    {
                        try
                        {
                            MediaPlayer.Stop();
                        }
                        catch (Exception ex)
                        {

                        }
                        index++;
                        mapLoadTask = mapLoader.LoadMapsAsync();
                        BackgroundTexture = (AssetLoader.LoadTextureUncached("Dynamicbg/loadingscreen/loading.jpg"));
                    }
                }
                else
                    time++;
            }
            base.Update(gameTime);

            if (updaterInitTask == null || updaterInitTask.Status == TaskStatus.RanToCompletion)
            {
                if (mapLoadTask?.Status == TaskStatus.RanToCompletion && (!UserINISettings.Instance.video_wallpaper || ProgramConstants.跳过Logo || index > 123))
                {
                    outputDevice?.Stop();
                    themeSongLoad?.Dispose();
                    Finish();
                }
            }
        }
    }


}
