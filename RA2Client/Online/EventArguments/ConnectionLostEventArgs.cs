using System;

namespace Ra2Client.Online.EventArguments
{
    public class ConnectionLostEventArgs : EventArgs
    {
        public ConnectionLostEventArgs(string reason)
        {
            Reason = reason;
        }
        public string Reason { get; private set; }
    }
}
