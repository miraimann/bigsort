using System;
using System.Collections.Generic;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class SorterBySymbols
        : ISorterBySymbols
    {
        private readonly ILinesIndexator _linesIndexesFinder;
        private readonly IByteHashCoder _hashCoder;
        private readonly ISubSorter _subSorter;
        private readonly IConfig _config;

        private readonly IPool<int[]> _registersPool;
        private readonly IPool<byte[]> _tablesPool;

        public SorterBySymbols(
            ILinesIndexator linesIndexesFinder, 
            IByteHashCoder hashCoder,
            IPoolMaker poolMaker,
            ISubSorter subSorter,
            IConfig config)
        {
            _linesIndexesFinder = linesIndexesFinder;
            _hashCoder = hashCoder;
            _subSorter = subSorter;
            _config = config;

            _registersPool = poolMaker.Make(NewRegister);
            _tablesPool = poolMaker.Make(NewTable);
        }

        public void Sort(
                IReadOnlyList<byte> group,
                SortedOut output,
                IEnumerable<LineIndexes> actualLines = null) =>

            Sort(group, output,
                 actualLines ?? _linesIndexesFinder.FindIn(group),
                 0);

        public unsafe void Sort(
            IReadOnlyList<byte> group,
            SortedOut output,
            IEnumerable<LineIndexes> linesIndexeses,
            int deep)
        {
            using (var pooledRegister = _registersPool.Get())
            using (var pooledTable = _tablesPool.Get())
                fixed (byte* tablePtr = pooledTable.Value)
                {
                    var register = pooledRegister.Value;
                    var table = (LineIndexes*) tablePtr;

                    foreach (var x in linesIndexeses)
                    {
                        var offset = deep + 2;
                        if (offset > x.lettersCount)
                        
                        group[x.start + x.digitsCount + deep + 5]
                    }
                }
        }
        
        private int[] NewRegister() =>
            new int[_hashCoder.HashCodesCount];

        private byte[] NewTable() =>
            new byte[_config.BufferSize];
    }
}
