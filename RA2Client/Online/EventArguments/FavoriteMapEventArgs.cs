using System;
using Ra2Client.Domain.Multiplayer;

namespace Ra2Client.Online.EventArguments
{
    public class FavoriteMapEventArgs : EventArgs
    {
        public readonly Map Map;

        public FavoriteMapEventArgs(Map map)
        {
            Map = map;
        }
    }
}
