using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.Entity
{
    public class BadgeVo
    {
        public int exp { get; set; }

        public int nextLevelExp { get; set; }

        public int level { get; set; }

        public string badgeName { get; set; }

        public List<Badge> canUseBadges { get; set; }


    }
}
