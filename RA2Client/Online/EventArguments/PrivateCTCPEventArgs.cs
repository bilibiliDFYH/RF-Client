using System;

namespace Ra2Client.Online.EventArguments
{
    public class PrivateCTCPEventArgs : EventArgs
    {
        public PrivateCTCPEventArgs(string sender, string message)
        {
            Sender = sender;
            Message = message;
        }

        public string Sender { get; private set; }

        public string Message { get; private set; }
    }
}
