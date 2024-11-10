using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTAConfig.Entity
{
    public record ResResult<T>
    {
        public string message { get; set; }
        public string code { get; set; }
        public T data { get; set; }

       
    }
}
