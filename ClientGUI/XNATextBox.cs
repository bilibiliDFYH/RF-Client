using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientGUI
{
    public class XNATextBox : Rampastring.XNAUI.XNAControls.XNATextBox
    {
        public XNATextBox(WindowManager windowManager) : base(windowManager)
        {
            
        }

        public override void Initialize()
        {
            base.Initialize();
            Tip = new ToolTip(WindowManager, this);
            TextChanged += (_, _) =>
            {
                Tip.Text = Text;
            };
        }

        private ToolTip Tip { get; set; }

        
    }
}
