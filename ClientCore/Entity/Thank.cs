using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.Entity
{
    public record class Thank
    {
        public int id { get; set; }
        public string author { get; set; }
        public string content { get; set; }

    }
}
