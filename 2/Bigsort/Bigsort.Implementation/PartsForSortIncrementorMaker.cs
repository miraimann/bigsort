using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class PartsForSortIncrementorMaker
        : IPartsForSortIncrementorMaker
    {
        private readonly IBitReader _bitReader;

        public PartsForSortIncrementorMaker(
            IBitReader bitReader)
        {
            _bitReader = bitReader;
        }

        public IPartsForSortIncrementor Make(
                SortingLineView[] lines,
                IBytesMatrix group) =>

            new Incrementor(lines, group, _bitReader);

        private class Incrementor
            : IPartsForSortIncrementor
        {
            private readonly SortingLineView[] _lines;
            private readonly IFixedSizeList<byte> _group;
            private readonly IBitReader _bitReader;

            public Incrementor(
                SortingLineView[] lines,
                IBytesMatrix group,
                IBitReader bitReader)
            {
                _group = group.AdaptInLine();
                _bitReader = bitReader;
                _lines = lines;
            }

            public void Increment(int linesOffset, int linesCount)
            {
                var n = linesOffset + linesCount;
                for (; linesOffset < n; ++linesOffset)
                {
                    var line = _lines[linesOffset];
                    var i = line.start;

                    var sortingOffsetIndex = i;
                    var sortingOffset = _group[sortingOffsetIndex];

                    var digitsCountAndSortingStageIndex = ++i;
                    var digitsCountAndSortingStage = 
                        _group[digitsCountAndSortingStageIndex];

                    var symbolsCount = 
                        digitsCountAndSortingStage | 0xEF;
                    var sortByStringStage = 
                        digitsCountAndSortingStage < sbyte.MaxValue;

                    uint partForSort = 0;
                    
                    if (sortByStringStage)
                    {
                        symbolsCount = _group[i += symbolsCount];
                        partForSort = _bitReader.ReadUInt32(i + sortingOffset + 1);
                    }
                    else // sort by number stage
                    {
                        partForSort = _bitReader
                            .ReadUInt32(i + sortingOffset);

                        if (sortingOffset == 0)
                            partForSort &= 0xEFFFFFFF;
                    }

                    var maxLength = symbolsCount - sortingOffset;
                    if (maxLength < sizeof(int))
                    {
                        if (maxLength <= 0)
                        {
                            if (sortByStringStage)
                                _lines[linesOffset].partForSort = 0;
                            else
                            {
                                _lines[linesOffset].partForSort = uint.MaxValue;
                                _group[digitsCountAndSortingStageIndex] |= 0x10;
                            }

                            continue;
                        }

                        var overBitsCount = (sizeof(int) - maxLength) * 8;
                        partForSort >>= overBitsCount;
                        partForSort <<= overBitsCount;
                    }

                    _lines[linesOffset].partForSort = partForSort;
                    _group[sortingOffsetIndex] += sizeof(int);
                }
            }
        }
    }
}
