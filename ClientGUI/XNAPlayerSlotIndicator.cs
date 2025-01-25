using System.Collections.Generic;
using Localization;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    public enum PlayerSlotState
    {
        Empty,
        Unavailable,
        AI,
        NotReady,
        Ready,
        InGame,
        Warning,
        Error
    }

    public class XNAPlayerSlotIndicator : XNAIndicator<PlayerSlotState>
    {
        public static new Dictionary<PlayerSlotState, Texture2D> Textures { get; set; }

        public ToolTip ToolTip { get; set; }

        public XNAPlayerSlotIndicator(WindowManager windowManager) : base(windowManager, Textures)
        {
        }

        public static void LoadTextures()
        {
            Textures = new Dictionary<PlayerSlotState, Texture2D>()
            {
                { PlayerSlotState.Empty, AssetLoader.LoadTextureUncached("statusEmpty.png") },
                { PlayerSlotState.Unavailable, AssetLoader.LoadTextureUncached("statusUnavailable.png") },
                { PlayerSlotState.AI, AssetLoader.LoadTextureUncached("statusAI.png") },
                { PlayerSlotState.NotReady, AssetLoader.LoadTextureUncached("statusClear.png") },
                { PlayerSlotState.Ready, AssetLoader.LoadTextureUncached("statusOk.png") },
                { PlayerSlotState.InGame, AssetLoader.LoadTextureUncached("statusInProgress.png") },
                { PlayerSlotState.Warning, AssetLoader.LoadTextureUncached("statusWarning.png") },
                { PlayerSlotState.Error, AssetLoader.LoadTextureUncached("statusError.png") }
            };
        }

        public override void Initialize()
        {
            base.Initialize();

            ToolTip = new ToolTip(WindowManager, this);
        }

        public override void SwitchTexture(PlayerSlotState key)
        {
            base.SwitchTexture(key);

            switch (key)
            {
                case PlayerSlotState.Empty:
                    //ToolTip.Text = "The slot is empty.".L10N("UI:ClientGUI:SlotEmpty");
                    ToolTip.Text = "该位置为空";
                    break;

                case PlayerSlotState.Unavailable:
                    //ToolTip.Text = "The slot is unavailable.".L10N("UI:ClientGUI:SlotUnavailable");
                    ToolTip.Text = "该位置已关闭";
                    break;

                case PlayerSlotState.AI:
                    //ToolTip.Text = "The player is computer-controlled.".L10N("UI:ClientGUI:PlayerIsComputer");
                    ToolTip.Text = "该位置由AI控制";
                    break;

                case PlayerSlotState.NotReady:
                    //ToolTip.Text = "The player isn't ready.".L10N("UI:ClientGUI:PlayerIsNotReady");
                    ToolTip.Text = "玩家未准备";
                    break;

                case PlayerSlotState.Ready:
                    //ToolTip.Text = "The player is ready.".L10N("UI:ClientGUI:PlayerIsReady");
                    ToolTip.Text = "玩家已就绪";
                    break;

                case PlayerSlotState.InGame:
                    //ToolTip.Text = "The player is in game.".L10N("UI:ClientGUI:PlayerIsInGame");
                    ToolTip.Text = "玩家正在游戏中";
                    break;

                case PlayerSlotState.Warning:
                    //ToolTip.Text = "The player has some issue(s) that may impact gameplay.".L10N("UI:ClientGUI:PlayerHasIssue");
                    ToolTip.Text = "该玩家可能出现了一些影响游戏玩法的问题";
                    break;

                case PlayerSlotState.Error:
                    //ToolTip.Text = "There's a critical issue with the player.".L10N("UI:ClientGUI:PlayerHasCriticalIssue");
                    ToolTip.Text = "该玩家存在严重问题";
                    break;
            }
        }
    }
}
