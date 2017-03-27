using System;
using System.Collections.Generic;

namespace Bigsort.Contracts
{
    internal interface ILinesIndexator
    {
        void IndexLines(
            IEnumerable<byte> source,
            Action<long> outLinesStart,
            Action<int> outDotShift);
    }
}
