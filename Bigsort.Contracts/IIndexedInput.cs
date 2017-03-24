using System.Collections.Generic;

namespace Bigsort.Contracts
{
    internal interface IIndexedInput
    {
        IReadOnlyList<long> LinesStarts { get; }
        IReadOnlyList<long> LinesEnds { get; }
        IReadingStream Bytes { get; }
    }
}
