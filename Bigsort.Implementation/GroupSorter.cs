using System;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GroupSorter<TSegment>
        : IGroupSorter

        where TSegment : IEquatable<TSegment>
                       , IComparable<TSegment>
    {
        private readonly ISortingSegmentsSupplier _segmentsSupplier;
        private readonly ILinesIndexesExtractor _linesIndexesExtractor;
        private readonly ILinesStorage<TSegment> _linesStorage;
        private readonly TSegment _lineSegmentsOut;
        public GroupSorter(
            ISortingSegmentsSupplier segmentsSupplier,
            ILinesIndexesExtractor linesIndexesExtractor, 
            ILinesStorage<TSegment> linesStorage,
            ISegmentService<TSegment> segmentService)
        {
            _segmentsSupplier = segmentsSupplier;
            _linesIndexesExtractor = linesIndexesExtractor;
            _linesStorage = linesStorage;
            _lineSegmentsOut = segmentService.DigitsOut;
        }

        public void Sort(IGroupBytesMatrix group, Range linesRange)
        {
            _linesIndexesExtractor.ExtractIndexes(group, linesRange);
            Sort(group, linesRange.Offset, linesRange.Length);
        }

        private void Sort(
            IGroupBytesMatrix group, 
            int offset, 
            int length)
        {
            var lines = _linesStorage.Indexes;
            var segments = _linesStorage.Segments;               // var dbg0 = Dbg.View(segments, offset, length);

            _segmentsSupplier.SupplyNext(group, offset, length); // var dbg1 = Dbg.View(segments, offset, length);
            Array.Sort(segments, lines, offset, length);         // var dbg2 = Dbg.View(segments, offset, length);

            int n = offset + length;
            while (offset < n)
            {
                int i = offset;
                TSegment current = segments[i], next = segments[++i];
                while (i < n && current.Equals(next) &&
                       !next.Equals(_lineSegmentsOut))
                    next = segments[++i];

                i -= offset;
                if (i != 1) Sort(group, offset, i);
                offset += i;
            }
        }
    }
}
