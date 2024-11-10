using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.Entity
{
    public record class Updater
    {
        public int id { get; set; }
        public string version { get; set; }
        public string file { get; set; }
        public string hash { get; set; }
        public long size { get; set; }
        public string log { get; set; }
        public string channel { get; set; }
        public string updateTime { get; set; }

    }
}
