using System.Collections.Generic;

namespace Bigsort.Contracts
{
    public interface ISubSorter
    {
        void Sort(IReadOnlyList<byte> group,
                  SortedOut output,
                  IEnumerable<LineIndexes> actualLines = null);
    }
}
