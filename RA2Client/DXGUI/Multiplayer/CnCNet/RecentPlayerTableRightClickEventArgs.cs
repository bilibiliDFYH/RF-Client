using System;
using Ra2Client.Online;

namespace Ra2Client.DXGUI.Multiplayer.CnCNet
{
    public class RecentPlayerTableRightClickEventArgs : EventArgs
    {
        public IRCUser IrcUser { get; set; }

        public RecentPlayerTableRightClickEventArgs(IRCUser ircUser)
        {
            IrcUser = ircUser;
        }
    }
}
