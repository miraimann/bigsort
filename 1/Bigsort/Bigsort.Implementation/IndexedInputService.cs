using System;
using System.Collections;
using System.Collections.Generic;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class IndexedInputService
        : IIndexedInputService
    {
        private readonly IConfig _config;

        public IndexedInputService(IConfig config)
        {
            _config = config;
        }

        public IIndexedInput MakeInput(
                IReadOnlyList<long> linesStarts,
                IReadingStream input) => 
            
            new Input(linesStarts, input);

        public IIndexedInput DecorateForStringsSorting(
                IIndexedInput core,
                IReadOnlyList<int> dotsShifts) =>
            
            new InputForStringsSorting(core, dotsShifts, _config);

        public IIndexedInput DecorateForNumbersSorting(
                IIndexedInput core,
                IReadOnlyList<int> dotsShifts) =>

            new InputForNumbersSorting(core, dotsShifts);
        
        private class Input
            : IIndexedInput
        {
            public Input(
                IReadOnlyList<long> linesStarts,
                IReadingStream input)
            {
                LinesStarts = linesStarts;
                LinesEnds = new ReadOnlyList(
                    LinesStarts.Count, 
                    GetLineEndByIndex);
                Bytes = input;
            }

            public IReadOnlyList<long> LinesStarts { get; }
            public IReadOnlyList<long> LinesEnds { get; }
            public IReadingStream Bytes { get; }

            private long GetLineEndByIndex(int i) =>
                (i < LinesStarts.Count - 1
                    ? LinesStarts[i + 1]
                    : Bytes.Length)
                    - 1;
        }

        private class InputForStringsSorting
            : IIndexedInput
        {
            private readonly IReadOnlyList<int> _dotsShifts;
            private readonly IConfig _config;
            private readonly IIndexedInput _core;

            public InputForStringsSorting(
                IIndexedInput core, 
                IReadOnlyList<int> dotsShifts,
                IConfig config)
            {
                _core = core;
                _dotsShifts = dotsShifts;
                _config = config;

                LinesStarts = new ReadOnlyList(
                    _core.LinesStarts.Count, 
                    GetLineStartByIndex);

                LinesEnds = new ReadOnlyList(
                    _core.LinesStarts.Count,
                    GetLineEndByIndex);
            }

            public IReadOnlyList<long> LinesStarts { get; }

            public IReadOnlyList<long> LinesEnds { get; }

            public IReadingStream Bytes =>
                _core.Bytes;

            private long GetLineStartByIndex(int i) =>
                _core.LinesStarts[i] 
                    + _dotsShifts[i]
                    + 1;

            private long GetLineEndByIndex(int i)
            {
                if (i == _core.LinesEnds.Count - 1)
                {
                    if (Bytes.Length < _config.EndLine.Length)
                        return _core.LinesEnds[i];

                    var end = new byte[_config.EndLine.Length];

                    var prevPosition = Bytes.Position;
                    Bytes.Position = Bytes.Length - end.Length;
                    Bytes.Read(end, 0, end.Length);
                    Bytes.Position = prevPosition;

                    for (int j = 0; j < end.Length; j++)
                        if (end[j] != _config.EndLine[j])
                            return _core.LinesEnds[i];
                }

                return _core.LinesEnds[i] - _config.EndLine.Length;
            }
        }

        private class InputForNumbersSorting
            : IIndexedInput
        {
            private readonly IDictionary<int, int> _zerosPrefixesLengths;
            private readonly IReadOnlyList<int> _dotsShifts;
            private readonly IIndexedInput _core;

            public InputForNumbersSorting(
               IIndexedInput core, IReadOnlyList<int> dotsShifts)
            {
                _core = core;
                _dotsShifts = dotsShifts;

                _zerosPrefixesLengths = new Dictionary<int, int>();

                LinesStarts = new ReadOnlyList(
                    _core.LinesStarts.Count,
                    GetLineStartByIndex);

                LinesEnds = new ReadOnlyList(
                    _core.LinesStarts.Count,
                    GetLineEndByIndex);
            }

            public IReadOnlyList<long> LinesStarts { get; }

            public IReadOnlyList<long> LinesEnds { get; }

            public IReadingStream Bytes =>
                _core.Bytes;

            private long GetLineEndByIndex(int i) =>
                _core.LinesStarts[i]
                    + _dotsShifts[i]
                    - 1;

            private long GetLineStartByIndex(int i)
            {
                if (!_zerosPrefixesLengths.ContainsKey(i))
                {
                    int j = 0;
                    Bytes.Position = _core.LinesStarts[i];

                    while (Bytes.ReadByte() == '0')
                        j++;

                    if (j == _dotsShifts[i])
                        j--;
                    
                    if (j != 0)
                        _zerosPrefixesLengths.Add(i, j);
                }

                return _zerosPrefixesLengths.ContainsKey(i)
                     ? _core.LinesStarts[i] + _zerosPrefixesLengths[i]
                     : _core.LinesStarts[i];
            }
        }

        private class ReadOnlyList
            : IReadOnlyList<long>
        {
            private readonly Func<int, long> _itemsProvider;

            public ReadOnlyList(int count, Func<int, long> itemsProvider)
            {
                _itemsProvider = itemsProvider;
                Count = count;
            }

            public int Count { get; }

            public long this[int i] =>
                _itemsProvider(i);

            public IEnumerator<long> GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                    yield return this[i];
            }

            IEnumerator IEnumerable.GetEnumerator() =>
                GetEnumerator();
        }
    }
}
