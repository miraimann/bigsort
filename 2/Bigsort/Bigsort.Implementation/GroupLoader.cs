using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GroupLoader
        : IGroupLoader
    {
        private readonly IBuffersPool _buffersPool;
        private readonly bool _isLittleEndian;

        public GroupLoader(
            IBuffersPool buffersPool,
            IConfig config)
        {
            _buffersPool = buffersPool;
            _isLittleEndian = config.IsLittleEndian;
        }
        
        public int LinesCountOfGroup(string path)
        {
            var buff = new byte[sizeof(int)];
            using (var stream = File.OpenRead(path))
                stream.Read(buff, 0, buff.Length);

            return BitConverter.ToInt32(buff, 0);
        }
        
        public IGroup Load(IGroupInfo seed) =>
            new Group(seed, _buffersPool, _isLittleEndian);

        private class Group 
            : IGroup
        {
            private readonly IPooled<byte[]>[] _buffHandles;
            private readonly Func<int, ulong> _read8Bytes;
            private readonly Func<int, uint> _read4Bytes;
            private readonly Func<ulong, ulong, int, ulong> _marge8Bytes;
            private readonly Func<uint, uint, int, uint> _marge4Bytes;
            
            public Group(
                IGroupInfo seed, 
                IPool<byte[]> buffersPool,
                bool isLittleEndian)
            {
                Name = seed.Name;
                BytesCount = seed.BytesCount;
                LinesCount = seed.LinesCount;
                ContentRowLength = seed.ContentRowLength;
                ContentRowsCount = seed.ContentRowsCount;
                
                using (var stream = File.OpenRead(seed.Name))
                {
                    _buffHandles = new IPooled<byte[]>[ContentRowsCount];
                    Content = new byte[ContentRowsCount][];

                    for (int i = 0; i < ContentRowsCount; i++)
                    {
                        _buffHandles[i] = buffersPool.Get();
                        Content[i] = _buffHandles[i].Value;
                        stream.Read(Content[i], 0, ContentRowLength);
                    }
                }

                if (isLittleEndian)
                {
                    _marge8Bytes = MargeLittleEndian8Bytes;
                    _read8Bytes = ReverseReadUInt64;

                    _marge4Bytes = MargeLittleEndian4Bytes;
                    _read4Bytes = ReverseReadUInt32;
                }
                else
                {
                    _marge8Bytes = MargeBigEndian8Bytes;
                    _read8Bytes = ReadUInt64;

                    _marge4Bytes = MargeBigEndian4Bytes;
                    _read4Bytes = ReadUInt32;
                }
            }

            public byte[][] Content { get; }
            public string Name { get; }
            public int ContentRowsCount { get; }
            public int ContentRowLength { get; }
            public int LinesCount { get; }
            public int BytesCount { get; }

            int IReadOnlyCollection<byte>.Count =>
                BytesCount;

            public byte this[int i]
            {
                get { return Content[i / ContentRowsCount][i % ContentRowsCount]; }
                set { Content[i / ContentRowsCount][i % ContentRowsCount] = value; }
            }

            public void DisposeRow(int i) =>
                _buffHandles[i].Dispose();

            public IEnumerator<byte> GetEnumerator() =>
                Content.Select(Enumerable.AsEnumerable)
                       .Aggregate(Enumerable.Concat)
                       .Take(BytesCount)
                       .GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() =>
                GetEnumerator();
            
            public ulong Read8Bytes(int i) =>
                _read8Bytes(i);
            
            public uint Read4Bytes(int i) =>
                _read4Bytes(i);

            private ulong ReadUInt64(int i)
            {
                int cell = i % ContentRowLength,
                     row = i / ContentRowLength;

                var result = BitConverter.ToUInt64(Content[row], cell);
                var offset = ContentRowLength - cell;
                if (offset > sizeof(ulong)) // is not broken to two rows
                    return result;

                var nextRow = row + 1;
                var additionBytes =
                    nextRow < ContentRowsCount
                        ? BitConverter.ToUInt64(Content[nextRow], 0)
                        : 0;

                return _marge8Bytes(result, additionBytes, offset * 8);
            }

            private uint ReadUInt32(int i)
            {
                int cell = i % ContentRowLength,
                     row = i / ContentRowLength;

                var result = BitConverter.ToUInt32(Content[row], cell);
                var offset = ContentRowLength - cell;
                if (offset > sizeof(uint)) // is not broken to two rows
                    return result;

                var nextRow = row + 1;
                var additionBytes =
                    nextRow < ContentRowsCount
                        ? BitConverter.ToUInt32(Content[nextRow], 0)
                        : 0;
                
                return _marge4Bytes(result, additionBytes, offset * 8);
            }

            private ulong ReverseReadUInt64(int i) =>
                Reverse(ReadUInt64(i));

            private uint ReverseReadUInt32(int i) =>
                Reverse(ReadUInt32(i));

            private ulong MargeLittleEndian8Bytes(
                    ulong value, ulong additionBytes, int offset) 
                =>
                (value & (ulong.MaxValue << offset)) |
                          (additionBytes << offset);

            private ulong MargeBigEndian8Bytes(
                    ulong value, ulong additionBytes, int offset)
                =>
                (value & (ulong.MaxValue >> offset)) |
                          (additionBytes >> offset);
            
            private uint MargeLittleEndian4Bytes(
                    uint value, uint additionBytes, int offset)
                =>
                (value & (uint.MaxValue << offset)) |
                         (additionBytes << offset);

            private uint MargeBigEndian4Bytes(
                    uint value, uint additionBytes, int offset)
                =>
                (value & (uint.MaxValue >> offset)) |
                         (additionBytes >> offset);
            
            private static ulong Reverse(ulong x) =>
                 ((x & 0xFF00000000000000) >> 56)
               | ((x & 0x00FF000000000000) >> 40)
               | ((x & 0x0000FF0000000000) >> 24)
               | ((x & 0x000000FF00000000) >> 8)
               | ((x & 0x00000000FF000000) << 8)
               | ((x & 0x0000000000FF0000) << 24)
               | ((x & 0x000000000000FF00) << 40)
               | ((x & 0x00000000000000FF) << 56);

            private static uint Reverse(uint x) =>
                 ((x & 0xFF000000) >> 24)
               | ((x & 0x00FF0000) >> 8)
               | ((x & 0x0000FF00) << 8)
               | ((x & 0x000000FF) << 24);
        }
    }
}
