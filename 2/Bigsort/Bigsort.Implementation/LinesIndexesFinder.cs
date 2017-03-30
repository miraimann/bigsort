using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class LinesIndexesFinder
        : ILinesIndexator
    {
        public IEnumerable<LineIndexes> FindIn(IReadOnlyList<byte> group)
        {
            var x = new LineIndexes();
            int i = 0;
            
            while (true)
            {
                x.start = i;
                x.lettersCount = (ushort)(group[i] * byte.MaxValue);
                x.digitsCount = group[++i];

                if (i == group.Count)
                    yield break;
                
                x.lettersCount += group[i += x.digitsCount];

                yield return x;

                i += x.lettersCount;
            }
        }
    }
}
