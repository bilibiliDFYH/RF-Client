using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ra2Client.Domain.Multiplayer
{
    public class TeamStartMappingPreset
    {
        [JsonInclude]
        [JsonPropertyName("n")]
        public string Name { get; set; }

        [JsonInclude]
        [JsonPropertyName("m")]
        public List<TeamStartMapping> TeamStartMappings { get; set; }
    }
}
