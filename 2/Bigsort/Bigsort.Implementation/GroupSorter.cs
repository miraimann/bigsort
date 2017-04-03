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
        private readonly LineIndexes[] _lines;
        private readonly TSegment[] _segments;
        private readonly TSegment _lineSegmentsOut;
        
        public GroupSorter(
            ISortingSegmentsSupplier segmentsSupplier,
            ILinesIndexesExtractor linesIndexesExtractor, 
            ILinesStorage<TSegment> linesStorage,
            ISegmentService<TSegment> segmentService)
        {
            _segmentsSupplier = segmentsSupplier;
            _linesIndexesExtractor = linesIndexesExtractor;
            _lines = linesStorage.Indexes;
            _segments = linesStorage.Segments;
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
            _segmentsSupplier.SupplyNext(group, offset, length);
            Array.Sort(_segments, _lines, offset, length);

            int n = offset + length;
            while (offset < n)
            {
                int i = offset;
                TSegment current = _segments[i], next = _segments[++i];
                while (i < n && current.Equals(next) &&
                       !next.Equals(_lineSegmentsOut))
                    next = _segments[++i];

                i -= offset;
                if (i != 1) Sort(group, offset, i);
                offset += i;
            }
        }
    }
}
