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

        public void SupplyNext(IGroupBytes group, Range linesRange)
        {
            var offset = linesRange.Offset;
            var n = offset + linesRange.Length;
            for (; offset < n; ++offset)
            {
                var line = _lines[offset];
                var i = line.start;

                TSegment x;
                byte symbolsCount;

                if (line.sortByDigits)
                {
                    symbolsCount = line.digitsCount;
                    x = Read(group, i + 2);
                }
                else
                {
                    symbolsCount = line.lettersCount;
                    x = Read(group, i + line.digitsCount + 3);
                }

                var maxLength = symbolsCount - line.sortingOffset;
                if (maxLength < _segmentSize)
                    x = maxLength <= 0
                        ? (line.sortByDigits ? _digitsOut : _lettersOut)
                        : _segment.ShiftRight(x, _segmentSize - maxLength);

                line.sortingOffset += _segmentSize;
                _segments[offset] = x;
            }
        }

        private TSegment Read(IGroupBytes group, int i)
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
