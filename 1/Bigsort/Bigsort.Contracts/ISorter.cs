using System.Collections.Generic;

namespace Bigsort.Contracts
{
    internal interface ISorter
    {
        void Sort(IEnumerable<int> actualLines);
    }
}
