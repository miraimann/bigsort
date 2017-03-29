using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GroupSorter
        //: IGroupSorter
    {
        public void Sort(IBytesMatrix group)
        {
            var sortingTree = new SortedDictionary<ulong, List<int>>();

            //for (int i = 0; i < group.BytesCount - 2;)
            //{
            //    int line = i;
            //    ushort letersCount = (ushort)(group.GetByte(i) * byte.MaxValue);
            //    byte digitsCount = group.GetByte(++i);
            //    letersCount += group.GetByte(i += digitsCount);
                
                
            //}
        }
    }
}
