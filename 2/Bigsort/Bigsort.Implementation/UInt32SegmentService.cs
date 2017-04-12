using System;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class UInt32SegmentService
        : ISegmentService<uint>
    {
        private readonly Func<byte[], int, uint> _read;

        public UInt32SegmentService()
        {
            if (BitConverter.IsLittleEndian)
                _read = ReverseRead;
            else _read = DirectRead;
        }

        public byte SegmentSize { get; } = sizeof(uint);
        public uint LettersOut { get; } = uint.MinValue;
        public uint DigitsOut { get; } = uint.MaxValue;

        public uint ShiftLeft(uint value, int bytesCount) =>
            value << (bytesCount * 8);

        public uint ShiftRight(uint value, int bytesCount) =>
            value >> (bytesCount * 8);

        public uint Read(byte[] buff, int offset) =>
            _read(buff, offset);

        public uint Merge(uint a, uint b) =>
            a | b;

        private static uint DirectRead(byte[] buff, int offset) =>
            BitConverter.ToUInt32(buff, offset);

        private static uint ReverseRead(byte[] buff, int offset) =>
            Reverse(DirectRead(buff, offset));

        private static uint Reverse(uint x) =>
              ((x & 0xFF000000) >> 24)
            | ((x & 0x00FF0000) >> 8)
            | ((x & 0x0000FF00) << 8)
            | ((x & 0x000000FF) << 24);
    }
}
