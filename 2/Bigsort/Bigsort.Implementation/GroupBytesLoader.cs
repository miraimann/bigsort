using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GroupBytesLoader
        : IGroupBytesLoader
    {
        private readonly IBuffersPool _buffersPool;
        public GroupBytesLoader(IBuffersPool buffersPool)
        {
            _buffersPool = buffersPool;
        }
        
        public IGroupBytes Load(IGroupInfo seed) =>
            new Group(seed, _buffersPool);

        private class Group 
            : IGroupBytes
        {
            private readonly Action _dispose;
            
            public Group(IGroupInfo seed, IBuffersPool buffersPool)
            {
                Name = seed.Name;
                BytesCount = seed.BytesCount;
                LinesCount = seed.LinesCount;
                RowLength = seed.RowLength;
                RowsCount = seed.RowsCount;
                
                using (var stream = File.OpenRead(seed.Name))
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
                get { return Rows[i / RowsCount][i % RowsCount]; }
                set { Rows[i / RowsCount][i % RowsCount] = value; }
            }

            public IEnumerator<byte> GetEnumerator() =>
                Rows.Select(Enumerable.AsEnumerable)
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
