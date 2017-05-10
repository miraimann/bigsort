using System;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class GroupSorter
        : IGroupSorter
    {
        private readonly ISortingSegmentsSupplier _segmentsSupplier;
        private readonly ILinesIndexesExtractor _linesIndexesExtractor;
        
        public GroupSorter(
            ISortingSegmentsSupplier segmentsSupplier,
            ILinesIndexesExtractor linesIndexesExtractor)
        {
            _segmentsSupplier = segmentsSupplier;
            _linesIndexesExtractor = linesIndexesExtractor;
        }

        public void Sort(IGroup group)
        {
            _linesIndexesExtractor.ExtractIndexes(group);
            Sort(group, group.Lines.Offset, group.Lines.Count);
        }

        private void Sort(IGroup group, int offset, int length)
        {
            var lines = group.Lines.Array;
            var segments = group.SortingSegments.Array;

            _segmentsSupplier.SupplyNext(group, offset, length);
            Array.Sort(segments, lines, offset, length);

            int n = offset + length;
            while (offset < n)
            {
                int i = offset;
                ulong current = segments[i], next = segments[++i];
                while (i < n && current.Equals(next) &&
                       !next.Equals(Consts.SegmentDigitsOut))
                    next = segments[++i];

                i -= offset;
                if (i != 1) Sort(group, offset, i);
                offset += i;
            }
        }
    }
}