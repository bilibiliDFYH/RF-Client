using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace Ra2Client.Domain.Multiplayer
{
    public class CustomMapCache
    {
        [JsonInclude]
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonInclude]
        [JsonPropertyName("maps")]
        public ConcurrentDictionary<string, Map> Maps { get; set; }
    }
}
