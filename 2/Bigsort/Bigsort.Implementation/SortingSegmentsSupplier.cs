using System;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class SortingSegmentsSupplier<TSegment>
        : ISortingSegmentsSupplier
        
        where TSegment : IEquatable<TSegment>
                       , IComparable<TSegment>
    {
        private readonly LineIndexes[] _lines;
        private readonly TSegment[] _segments;
        private readonly TSegment _digitsOut, _lettersOut;
        private readonly byte _segmentSize;
        private readonly ISegmentService<TSegment> _segment;

        public SortingSegmentsSupplier(
            ILinesStorage<TSegment> linesStorage,
            ISegmentService<TSegment> segmentService)
        { 
            _lines = linesStorage.Indexes;
            _segments = linesStorage.Segments;

            _segment = segmentService;
            _digitsOut = _segment.DigitsOut;
            _lettersOut = _segment.LettersOut;
            _segmentSize = _segment.SegmentSize;
        }

        public void SupplyNext(IGroupBytesMatrix group, Range linesRange) =>
            SupplyNext(group, linesRange.Offset, linesRange.Length);

        public void SupplyNext(IGroupBytesMatrix group, int offset, int count)
        {
            var n = offset + count;
            for (; offset < n; ++offset)
            {
                var line = _lines[offset];
                var i = line.start;

                TSegment x;
                // byte symbolsCount;
                int maxLength = (line.sortByDigits
                              ?  line.digitsCount
                              :  line.lettersCount)
                              -  line.sortingOffset;
                if (maxLength <= 0)
                    x = line.sortByDigits 
                      ? _digitsOut 
                      : _lettersOut;
                else
                {
                    x = line.sortByDigits 
                      ? Read(group, i + 2 + line.sortingOffset)
                      : Read(group, i + 3 + line.sortingOffset + line.digitsCount);

                    if (maxLength < _segmentSize)
                        x = _segment.ShiftRight(x, _segmentSize - maxLength);
                }

                line.sortingOffset += _segmentSize;
                _lines[offset] = line;
                _segments[offset] = x;
            }
        }

        private TSegment Read(IGroupBytesMatrix group, int i)
        {
            int rowLength = group.RowLength,
                cellIndex = i % rowLength,
                 rowIndex = i / rowLength;

            var result = _segment.Read(group.Rows[rowIndex], cellIndex);
            var rowLeftLength = rowLength - cellIndex;
            if (rowLeftLength > _segmentSize) // is not broken to two rows
                return result;

            var offset = _segmentSize - rowLeftLength;
            result = _segment.ShiftRight(result, offset);
            result = _segment.ShiftLeft(result, offset);

            if (++rowIndex < group.RowsCount)
            {
                TSegment additionBytes = _segment.Read(group.Rows[rowIndex], 0);
                additionBytes = _segment.ShiftLeft(additionBytes, rowLeftLength);
                result = _segment.Merge(result, additionBytes);
            }

            return result;
        }
    }
}
