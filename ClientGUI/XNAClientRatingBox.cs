using System;
using Localization;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    public class XNAClientRatingBox : XNARatingBox
    {
        public ToolTip ToolTip { get; set; }

        public XNAClientRatingBox(WindowManager windowManager) : base(windowManager)
        {
        }

        private void CreateToolTip()
        {
            if (ToolTip == null)
                ToolTip = new ToolTip(WindowManager, this);
        }

        public override void Initialize()
        {
            CheckSoundEffect = new EnhancedSoundEffect("checkbox.wav");

            base.Initialize();

            CreateToolTip();
        }

        public override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == "ToolTip")
            {
                CreateToolTip();
                ToolTip.Text = value.Replace("@", Environment.NewLine);
                return;
            }
            if (key == "$ToolTip")
            {
                CreateToolTip();
                ToolTip.Text = string.Empty.L10N("UI:Main:" + value).Replace("@", Environment.NewLine);
                return;
            }
            base.ParseControlINIAttribute(iniFile, key, value);
        }
    }
}
