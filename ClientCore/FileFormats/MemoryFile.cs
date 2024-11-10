using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTAConfig.Entity
{
    public class MemoryFile : VirtualFile
    {

        public MemoryFile(byte[] buffer, bool isBuffered = true) :
            base(new MemoryStream(buffer), "MemoryFile", 0, buffer.Length, isBuffered)
        { }
    }
}
