using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GroupBytesLoader
        : IGroupBytesLoader
    {
        private readonly IBuffersPool _buffersPool;
        private readonly IIoService _ioService;
        private readonly int _rowLength;

        public GroupBytesLoader(
            IBuffersPool buffersPool, 
            IIoService ioService,
            IConfig config)
        {
            _buffersPool = buffersPool;
            _ioService = ioService;
            _rowLength = config.BufferSize 
                       - config.GroupBufferRowReadingEnsurance;
        }

        public IGroupBytesMatrix LoadMatrix(IGroupBytesMatrixInfo seed) => 
            new GroupMatrix(seed, _buffersPool, _ioService);

        public IGroupBytesMatrixInfo CalculateMatrixInfo(IGroupInfo seed) => 
            new GroupMatrixInfo(seed, _rowLength);

        private class GroupMatrixInfo
            : IGroupBytesMatrixInfo
        {
            public GroupMatrixInfo(
                IGroupInfo seed, int rowLength)
            {
                Name = seed.Name;
                BytesCount = seed.BytesCount;
                LinesCount = seed.LinesCount;
                RowLength = rowLength;
                RowsCount = (BytesCount / RowLength) +
                            (BytesCount % RowLength == 0 ? 0 : 1);
            }

            public string Name { get; }
            public int LinesCount { get; }
            public int BytesCount { get; }
            public int RowsCount { get; }
            public int RowLength { get; }
        }

        private class GroupMatrix
            : IGroupBytesMatrix
        {
            private readonly Action _dispose;
            
            public GroupMatrix(
                IGroupBytesMatrixInfo seed, 
                IBuffersPool buffersPool,
                IIoService ioService)
            {
                Name = seed.Name;
                BytesCount = seed.BytesCount;
                LinesCount = seed.LinesCount;
                RowLength = seed.RowLength;
                RowsCount = seed.RowsCount;

                using (var stream = ioService.OpenRead(seed.Name))
                {
                    Rows = new byte[RowsCount][];

                    for (int i = 0; i < RowsCount; i++)
                    {
                        var handle = buffersPool.GetBuffer();
                        Rows[i] = handle.Value;
                        _dispose += handle.Dispose;
                        stream.Read(Rows[i], 0, RowLength);
                    }
                }
            }

            public byte[][] Rows { get; }
            public string Name { get; }
            public int RowsCount { get; }
            public int RowLength { get; }
            public int LinesCount { get; }
            public int BytesCount { get; }

            int IReadOnlyCollection<byte>.Count =>
                BytesCount;

            public byte this[int i]
            {
                get { return Rows[i / RowLength][i % RowLength]; }
                set { Rows[i / RowLength][i % RowLength] = value; }
            }

            public IEnumerator<byte> GetEnumerator() =>
                Rows.Select(row => row.Take(RowLength))
                    .Aggregate(Enumerable.Concat)
                    .Take(BytesCount)
                    .GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() =>
                GetEnumerator();

            public void Dispose() =>
                _dispose();
        }
    }
}
