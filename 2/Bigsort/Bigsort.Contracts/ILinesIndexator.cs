using System.Collections.Generic;

namespace Bigsort.Contracts
{
    public interface ILinesIndexator
    {
        IEnumerable<LineIndexes> FindIn(IReadOnlyList<byte> group);
    }
}
