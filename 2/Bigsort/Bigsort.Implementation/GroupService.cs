using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GroupService
        : IGroupService
    {
        private readonly IBuffersPool _buffersPool;
        private readonly IConfig _config;

        public GroupService(
            IBuffersPool buffersPool,
            IConfig config)
        {
            _config = config;
            _buffersPool = buffersPool;
        }
        
        public int LinesCountOfGroup(string path)
        {
            var buff = new byte[sizeof(int)];
            using (var stream = File.OpenRead(path))
                stream.Read(buff, 0, buff.Length);

            return BitConverter.ToInt32(buff, 0);
        }

        public IGroup LoadGroup(string path) =>
            new Group(path, _buffersPool, _config);

        private class Group 
            : IGroup
        {
            private readonly IPooled<byte[]>[] _buffHandles;

            private readonly Func<int, ulong> 
                _readLittleEndianUInt64,
                _readBigEndianUInt64;

            private readonly Func<int, uint>
                _readLittleEndianUInt32,
                _readBigEndianUInt32;
            
            private readonly Func<int, ushort>
                _readLittleEndianUInt16,
                _readBigEndianUInt16;
            
            public Group(
                string path,
                IPool<byte[]> buffersPool,
                IConfig config)
            {
                const int rowReadingEnsurance = 
                    sizeof(ulong) - 1;
                
                using (var stream = File.OpenRead(path))
                {
                    var linesCountBuff = new byte[sizeof(int)];
                    Count = (int)stream.Length - linesCountBuff.Length;
                    stream.Read(linesCountBuff, 0, linesCountBuff.Length);
                    LinesCount = BitConverter.ToInt32(linesCountBuff, 0);
                        
                    ContentRowLength = config.BufferSize - rowReadingEnsurance;
                    ContentRowsCount = (Count / ContentRowLength)
                                     + (Count % ContentRowLength == 0 ? 0 : 1);

                    _buffHandles = new IPooled<byte[]>[ContentRowsCount];
                    Content = new byte[ContentRowsCount][];

                    for (int i = 0; i < ContentRowsCount; i++)
                    {
                        _buffHandles[i] = buffersPool.Get();
                        Content[i] = _buffHandles[i].Value;
                        stream.Read(Content[i], 0, ContentRowLength);
                    }
                }

                if (BitConverter.IsLittleEndian)
                {
                    _readLittleEndianUInt64 = ReadUInt64;
                    _readBigEndianUInt64 = ReverseReadUInt64;
                    _readLittleEndianUInt32 = ReadUInt32;
                    _readBigEndianUInt32 = ReverseReadUInt32;
                    _readLittleEndianUInt16 = ReadUInt16;
                    _readBigEndianUInt16 = ReverseReadUInt16;
                }
                else
                {
                    _readLittleEndianUInt64 = ReverseReadUInt64;
                    _readBigEndianUInt64 = ReadUInt64;
                    _readLittleEndianUInt32 = ReverseReadUInt32;
                    _readBigEndianUInt32 = ReadUInt32;
                    _readLittleEndianUInt16 = ReverseReadUInt16;
                    _readBigEndianUInt16 = ReadUInt16;
                }
            }

            public byte[][] Content { get; }
            public int ContentRowsCount { get; }
            public int ContentRowLength { get; }
            public int Count { get; }
            public int LinesCount { get; }

            public byte this[int i]
            {
                get { return Content[i / ContentRowsCount][i % ContentRowsCount]; }
                set { Content[i / ContentRowsCount][i % ContentRowsCount] = value; }
            }

            public IEnumerator<byte> GetEnumerator() =>
                Content.Select(Enumerable.AsEnumerable)
                       .Aggregate(Enumerable.Concat)
                       .Take(Count)
                       .GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() =>
                GetEnumerator();

            public void Dispose()
            {
                foreach (var handle in _buffHandles)
                    handle.Dispose();
            }
            
            public ulong ReadLittleEndianUInt64(int i) =>
                _readLittleEndianUInt64(i);

            public ulong ReadBigEndianUInt64(int i) =>
                _readBigEndianUInt64(i);
            
            public uint ReadLittleEndianUInt32(int i) =>
                _readLittleEndianUInt32(i);

            public uint ReadBigEndianUInt32(int i) =>
                _readBigEndianUInt32(i);
            
            public ushort ReadLittleEndianUInt16(int i) =>
                _readLittleEndianUInt16(i);

            public ushort ReadBigEndianUInt16(int i) =>
                _readBigEndianUInt16(i);

            public ulong ReadUInt64(int i)
            {
                int cell = i % ContentRowLength,
                     row = i / ContentRowLength;

                var result = BitConverter.ToUInt64(Content[row], cell);
                var overBytesCount = cell + sizeof(ulong) - ContentRowLength;
                if (overBytesCount <= 0)
                    return result;

                var nextRow = row + 1;
                var overBytes = nextRow < ContentRowsCount
                    ? BitConverter.ToUInt64(Content[nextRow], 0)
                    : 0;

                return (result >> (overBytesCount * 8))
                     | (overBytes << (overBytesCount * 8));
            }

            public uint ReadUInt32(int i)
            {
                int cell = i % ContentRowLength,
                     row = i / ContentRowLength;

                var result = BitConverter.ToUInt32(Content[row], cell);
                var overBytesCount = cell + sizeof(uint) - ContentRowLength;
                if (overBytesCount <= 0)
                    return result;

                var overBytes = BitConverter
                   .ToUInt32(Content[row + 1], 0);

                return (result >> (overBytesCount * 8))
                     | (overBytes << (overBytesCount * 8));
            }

            public ushort ReadUInt16(int i)
            {
                int cell = i % ContentRowLength,
                     row = i / ContentRowLength;

                var result = BitConverter.ToUInt16(Content[row], cell);
                var overBytesCount = cell + sizeof(ushort) - ContentRowLength;
                if (overBytesCount <= 0)
                    return result;

                var overBytes = BitConverter
                   .ToUInt16(Content[row + 1], 0);

                return (ushort)(
                    (result >> (overBytesCount * 8)) |
                    (overBytes << (overBytesCount * 8))
                );
            }

            private ulong ReverseReadUInt64(int i) =>
                Reverse(ReadUInt64(i));

            private uint ReverseReadUInt32(int i) =>
                Reverse(ReadUInt32(i));

            private ushort ReverseReadUInt16(int i) =>
                Reverse(ReadUInt16(i));
            
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

            private static ushort Reverse(ushort x) =>
                (ushort)(
                  ((x & 0xFF00) >> 8)
                | ((x & 0x00FF) << 8)
                );
        }
    }
}
