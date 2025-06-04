using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.Entity
{
    public class Maps
    {
        public string img { get; set; } = null;

        public int id { get; set; } = 0;
        public string name { get; set; } = null;
        public string maxPlayers { get; set; } = null;
        public string author { get; set; } = null;
        public int type { get; set; } = 0;
        public string base64 { get; set; } = null;
        public string file { get; set; } = null;
        public string createTime { get; set; } = null;
        public int createUser { get; set; } = 0;
        public int enable { get; set; } = 0;
        public long downCount { get; set; } = 0;
        public double score { get; set; } = 0.0;
        public string description { get; set; } = "";
        public string uploadUserName { get; set; } = "";

        public string typeName { get; set; } = "";
        public string rules { get; set; }

        public string enemyHouse { get; set; } = "";

        public string allyHouse { get; set; } = "";

        public Maps() { }
    }

}
