using System;

namespace Ra2Client.Online.EventArguments
{
    public class UnreadMessageCountEventArgs : EventArgs
    {
        public int UnreadMessageCount { get; set; }

        public UnreadMessageCountEventArgs(int unreadMessageCount)
        {
            UnreadMessageCount = unreadMessageCount;
        }
    }
}
