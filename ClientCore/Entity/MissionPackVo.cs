using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ClientCore.Entity
{
    public record class MissionPackVo
    {
        public string? id { get; set; }

        public string name { get; set; } = "";

        public string? description { get; set; }

        public List<int>? camp { get; set; }

        public List<string>? tags { get; set; }

        public string file { get; set; } = "";

        public string? author { get; set; }

        public int missionCount { get; set; } = 1;

        public int? year { get; set; }

        public int gameType { get; set; }

        public string? link { get; set; }

        public string updateTime { get; set; }
    }

}
