using System;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class SortingSegmentsSupplier<TSegment>
        : ISortingSegmentsSupplier
        
        where TSegment : IEquatable<TSegment>
                       , IComparable<TSegment>
    {
        private readonly TSegment _digitsOut, _lettersOut;
        private readonly byte _segmentSize;
        private readonly ISegmentService<TSegment> _segment;
        private readonly ILinesStorage<TSegment> _linesStorage;

        public SortingSegmentsSupplier(
            ILinesStorage<TSegment> linesStorage,
            ISegmentService<TSegment> segmentService)
        {
            _linesStorage = linesStorage;
            _segment = segmentService;
            _digitsOut = _segment.DigitsOut;
            _lettersOut = _segment.LettersOut;
            _segmentSize = _segment.SegmentSize;
        }

        public void SupplyNext(IGroupBytesMatrix group, Range linesRange) =>
            SupplyNext(group, linesRange.Offset, linesRange.Length);

        public void SupplyNext(IGroupBytesMatrix group, int offset, int count)
        {
            var lines = _linesStorage.Indexes;
            var segments = _linesStorage.Segments;

            var n = offset + count;
            for (; offset < n; ++offset)
            {
                var line = lines[offset];
                TSegment x;

                int maxLength = (line.sortByDigits
                                    ? line.digitsCount + 1
                                    : line.lettersCount) -
                                 line.sortingOffset;
                if (maxLength <= 0)
                {
                    line.sortingOffset = 0;
                    if (line.sortByDigits)
                        x = _digitsOut;
                    else
                    {
                        x = _lettersOut;
                        line.sortByDigits = true;
                    }
                }
                else
                {
                    var lineReadingOffset = line.start 
                        +  line.sortingOffset
                        + (line.sortByDigits ? 1 : line.digitsCount + 3);

                    x = Read(group, lineReadingOffset);                             // var dbg1 = Dbg.View(x);
                    if (maxLength < _segmentSize)                                   
                    {                                                               
                        x = _segment.ShiftRight(x, _segmentSize - maxLength);       // var dbg2 = Dbg.View(x);
                        x = _segment.ShiftLeft(x, _segmentSize - maxLength);        // var dbg3 = Dbg.View(x);
                    }                                                               
                                                                                    
                    line.sortingOffset += _segmentSize;                             
                }                                                                   
                                                                                    
                lines[offset] = line;                                              
                segments[offset] = x;                                               // var dbg4 = Dbg.View(x);
            }
        }

        private TSegment Read(IGroupBytesMatrix group, int i)
        {
            int rowLength = group.RowLength,
                cellIndex = i % rowLength,
                 rowIndex = i / rowLength;

            var result = _segment.Read(group.Rows[rowIndex], cellIndex);            // var dbg1 = Dbg.View(result);
            var rowLeftLength = rowLength - cellIndex;
            if (rowLeftLength >= _segmentSize) // is not broken to two rows
                return result;

            var offset = _segmentSize - rowLeftLength;
            result = _segment.ShiftRight(result, offset);                           // var dbg2 = Dbg.View(result);
            result = _segment.ShiftLeft(result, offset);                            // var dbg3 = Dbg.View(result);

            if (++rowIndex < group.RowsCount)
            {
                TSegment additionBytes = _segment.Read(group.Rows[rowIndex], 0);    // var dbg4 = Dbg.View(additionBytes);
                additionBytes = _segment.ShiftRight(additionBytes, rowLeftLength);  // var dbg5 = Dbg.View(additionBytes);
                result = _segment.Merge(result, additionBytes);                     // var dbg6 = Dbg.View(result);
            }

            return result;
        }
    }
}
