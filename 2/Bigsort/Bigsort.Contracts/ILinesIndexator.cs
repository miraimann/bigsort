using System.Collections.Generic;

namespace Bigsort.Contracts
{
    public interface ILinesIndexator
    {
        void IndexLines(
            IReadOnlyList<byte> group,
            ArrayFragment<SortingLine> lines);
    }
}
