using System;
using System.Text.Json.Serialization;

namespace Ra2Client.Online
{
    public class RecentPlayer
    {
        [JsonInclude]
        public string PlayerName { get; set; }

        [JsonInclude]
        public string GameName { get; set; }

        [JsonInclude]
        public DateTime GameTime { get; set; }
    }
}
