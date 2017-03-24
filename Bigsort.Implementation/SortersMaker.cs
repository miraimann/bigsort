using System;
using System.Collections.Generic;
using System.Linq;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class SortersMaker
        : ISortersMaker
    {
        private readonly IAccumulatorsFactory _accumulatorsFactory;
        private readonly IPoolMaker _poolMaker;

        public SortersMaker(
            IAccumulatorsFactory accumulatorsFactory,
            IPoolMaker poolMaker)
        {
            _accumulatorsFactory = accumulatorsFactory;
            _poolMaker = poolMaker;
        }

        public ISorter MakeSymbolBySymbolSorter(
                IIndexedInput input,
                Action<int> nextLineFound,
                ISorter subSorter,
                Func<int, int> hashFunc = null,
                int abcLength = byte.MaxValue + 1) =>

            new SymbolBySymbolSorter(
                _accumulatorsFactory,
                _poolMaker,
                abcLength,
                nextLineFound, 
                input, 
                subSorter,
                hashFunc);


        public ISorter MakeLengthSorter(
                IIndexedInput input,
                Action<int> nextLineFound,
                ISorter subSorter) =>

            new ByLengthSorter(nextLineFound, input, subSorter);

        public ISorter MakeNoSortSorter(
                Action<int> nextLineFound) =>

            new NoSortSorter(nextLineFound);

        private class SymbolBySymbolSorter
            : ISorter
        {
            private readonly IAccumulatorsFactory _accumulatorsFactory;
            private readonly IPool<IAccumulator<int>[]> _groupsPool;
            private readonly Action<int> _outSortedLine;
            private readonly IIndexedInput _input;
            private readonly ISorter _subSorter;
            private readonly Func<int, int> _hashFunc;

            public SymbolBySymbolSorter(
                IAccumulatorsFactory accumulatorsFactory,
                IPoolMaker poolMaker,
                int differentSymbolsCount,
                Action<int> outSortedLine,
                IIndexedInput input,
                ISorter subSorter,
                Func<int, int> hashFunc)
            {
                _groupsPool = poolMaker.Make(
                    () => new IAccumulator<int>[differentSymbolsCount],
                    groups =>
                    {
                        foreach (var group in groups)
                            group?.Clear();
                    });

                _accumulatorsFactory = accumulatorsFactory;
                _outSortedLine = outSortedLine;
                _subSorter = subSorter;
                _hashFunc = hashFunc;
                _input = input;
            }

            public void Sort(IEnumerable<int> actualLines) =>
                Sort(actualLines, 0);

            private void Sort(IEnumerable<int> actualLines, int deep)
            {
                var finishedLines = new List<int>(4);
                var groups = _groupsPool.Get();

                foreach (var line in actualLines)
                {
                    var i = _input.LinesStarts[line] + deep;
                    if (i > _input.LinesEnds[line])
                        finishedLines.Add(line);
                    else
                    {
                        _input.Bytes.Position = i;
                        var x = _input.Bytes.ReadByte();
                        if (_hashFunc != null)
                            x = _hashFunc(x);

                        if (groups.Value[x] == null)
                            groups.Value[x] = _accumulatorsFactory.CreateForInt();
                        groups.Value[x].Add(line);
                    }
                }

                if (finishedLines.Count > 0)
                {
                    if (finishedLines.Count == 1)
                        _outSortedLine(finishedLines[0]);
                    else
                        _subSorter.Sort(finishedLines);
                }

                ++deep;
                foreach (var group in groups.Value)
                {
                    if (group == null || group.Count == 0)
                        continue;
                    if (group.Count == 1)
                        _outSortedLine(group[0]);
                    else Sort(group, deep);
                }

                groups.Free();
            }
        }

        private class ByLengthSorter
            : ISorter
        {
            private readonly Action<int> _outSortedLine;
            private readonly IIndexedInput _input;
            private readonly ISorter _subSorter;

            public ByLengthSorter(
                Action<int> outSortedLine, 
                IIndexedInput input, 
                ISorter subSorter)
            {
                _outSortedLine = outSortedLine;
                _input = input;
                _subSorter = subSorter;
            }

            public void Sort(IEnumerable<int> actualLines)
            {
                foreach (var group in actualLines
                                             .GroupBy(i => _input.LinesEnds[i] - 
                                                           _input.LinesStarts[i])
                                             .OrderBy(o => o.Key))
                    if (group.Skip(1).Any())
                        _subSorter.Sort(group);
                    else _outSortedLine(group.First());
            }
        }

        private class NoSortSorter
            : ISorter
        {
            private readonly Action<int> _outSortedLine;   
            public NoSortSorter(Action<int> outSortedLine)
            {
                _outSortedLine = outSortedLine;
            }

            public void Sort(IEnumerable<int> actualLines)
            {
                foreach (var line in actualLines)
                    _outSortedLine(line);
            }
        }
    }
}
