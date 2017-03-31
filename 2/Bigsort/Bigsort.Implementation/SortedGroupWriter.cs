using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class SortedGroupWriter
        : ISortedGroupWriter
    {
        public void Write(
            IGroup group, 
            ArrayFragment<SortingLine> lines, 
            IWriter output)
        {
            const int endLineLength = 1;
            const byte endLineByte1 = (byte) '\r',
                       endLineByte2 = (byte) '\n';
        }
    }
}
