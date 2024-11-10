using System;

namespace Ra2Client.Online.EventArguments
{
    public class ChannelTopicEventArgs : EventArgs
    {
        public ChannelTopicEventArgs(string channelName, string topic)
        {
            ChannelName = channelName;
            Topic = topic;
        }

        public string ChannelName { get; private set; }
        public string Topic { get; private set; }
    }
}
