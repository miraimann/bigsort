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

        public void Sort(IGroupBytes group, Range linesRange)
        {
            _linesIndexesExtractor
                .ExtractIndexes(group, linesRange);
            
            _segmentsSupplier
                .SupplyNext(group, linesRange);
            
            Array.Sort(_segments, _lines,
                linesRange.Offset,
                linesRange.Length);

            int offset = linesRange.Offset,
                n = offset + linesRange.Length - 1,
                next = 0,
                count = 1;

            TSegment 
                lineSegment = default(TSegment),
                nextLineSegment = default(TSegment);
            
            while (offset < n)
            {
                var isSingle = count == 1;
                if (isSingle)
                {
                    lineSegment = _segments[offset];
                    nextLineSegment = _segments[next = offset + 1];
                }

                if (nextLineSegment.Equals(lineSegment) &&
                    !nextLineSegment.Equals(_lineSegmentsOut))
                    nextLineSegment = _segments[next + ++count];
                else if (isSingle) offset++;
                else
                {
                    _segmentsSupplier.SupplyNext(group, linesRange);
                    Array.Sort(_segments, _lines, offset, count);
                    count = 1;
                }
            }
        }
    }
}
