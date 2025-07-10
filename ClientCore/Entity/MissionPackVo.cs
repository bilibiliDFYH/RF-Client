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
        public long? id { get; set; }

        public string name { get; set; } = "";

        public string? description { get; set; }

        public string? camp { get; set; }

        public string? tags { get; set; }

        public string file { get; set; } = "";

        public string? img { get; set; }

        public long? uploadUser { get; set; }

        public string? uploadUserName { get; set; } = "";

        public string? version { get; set; }

        public string? author { get; set; }

        public int missionCount { get; set; } = 1;

        public int? year { get; set; }

        public bool gameType { get; set; }

        public string? link { get; set; }

        public long downCount { get; set; } = 0;

        public bool enable { get; set; } = false;
    }

}
