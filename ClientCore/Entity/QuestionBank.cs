using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTAConfig.Entity
{
    public record class QuestionBank
    {
        public int? id { get; set; }
        public string name { get; set; } = "";
        public string problem { get; set; } = "";
        public string options { get; set; } = "";
        public int answer { get; set; } = 0;
        public int difficulty { get; set; } = 0;
        public string type { get; set; } = "";
        public int enable { get; set; } = 0;
    }
}
