using Bigsort.Contracts;
using System.Collections.Generic;

namespace Bigsort.Tests
{
    internal class IndexedInput
        : IIndexedInput
    {
        public IReadOnlyList<long> LinesStarts { get; set; }
        public IReadOnlyList<long> LinesEnds { get; set; }
        public IReadingStream Bytes { get; set; }
    }
}
