using System.Collections.Generic;

namespace Bigsort.Contracts
{
    internal struct LongRange
    {
        public long Offset;
        public int Length;

        public LongRange(long offset, int count)
        {
            Offset = offset;
            Length = count;
        }

        public static readonly IComparer<LongRange> ByOffsetComparer;

        static LongRange()
        {
            var longComparer = Comparer<long>.Default;
            ByOffsetComparer = Comparer<LongRange>.Create(
                (a, b) => longComparer.Compare(a.Offset, b.Offset));
        }
    }
}
