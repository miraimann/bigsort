using System.Collections.Generic;

namespace Bigsort.Contracts
{
    internal interface IResultWriter
    {
        void Write(
            IIndexedInput input,
            IWritingStream output,
            IReadOnlyList<int> linesOrdering);
    }
}
