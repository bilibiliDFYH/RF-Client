using System;

namespace Ra2Client.Online.EventArguments
{
    public class ChannelEventArgs : EventArgs
    {
        public ChannelEventArgs(string channelName)
        {
            ChannelName = channelName;
        }

        public string ChannelName { get; private set; }
    }
}
