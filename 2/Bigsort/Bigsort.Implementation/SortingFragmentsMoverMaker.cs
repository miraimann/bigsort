﻿using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class SortingFragmentsMoverMaker
        : ISortingFragmentsMoverMaker
    {
        public ISortingFragmentsMover Make(
                IGroup group, SortingLine[] lines) =>
            new Mover(lines, group);

        private class Mover
            : ISortingFragmentsMover
        {
            private readonly SortingLine[] _lines;
            private readonly IGroup _group;
            
            public Mover(SortingLine[] lines, IGroup group)
            {
                _group = group;
                _lines = lines;
            }
            
            public void MoveNext(int linesOffset, int linesCount)
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
                        digitsCountAndSortingStage & 0xEF;
                    var sortByStringStage = 
                        digitsCountAndSortingStage < sbyte.MaxValue;

                    uint partForSort;
                    
                    if (sortByStringStage)
                    {
                        symbolsCount = _group[i += symbolsCount];
                        partForSort = _group.Read4Bytes(i + sortingOffset + 1);
                    }
                    else // sort by number stage
                    {
                        partForSort = _group.Read4Bytes(i + sortingOffset);

                        if (sortingOffset == 0)
                            partForSort &= 0xEFFFFFFF;
                    }

                    var maxLength = symbolsCount - sortingOffset;
                    if (maxLength < sizeof(int))
                    {
                        if (maxLength <= 0)
                        {
                            if (sortByStringStage)
                                _lines[linesOffset].fragmentForSort = 0;
                            else
                            {
                                _lines[linesOffset].fragmentForSort = uint.MaxValue;
                                _group[digitsCountAndSortingStageIndex] |= 0x10;
                            }

                            continue;
                        }

                        var overBitsCount = (sizeof(int) - maxLength) * 8;
                        partForSort >>= overBitsCount;
                        partForSort <<= overBitsCount;
                    }

                    _lines[linesOffset].fragmentForSort = partForSort;
                    _group[sortingOffsetIndex] += sizeof(int);
                }
            }
        }
    }
}
