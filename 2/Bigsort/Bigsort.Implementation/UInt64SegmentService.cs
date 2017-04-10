using System;
using System.Runtime.CompilerServices;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class UInt64SegmentService
        : ISegmentService<ulong>
    {
        private readonly Func<byte[], int, ulong> _read;

        public UInt64SegmentService(bool isLittleEndian)
        {
            if (isLittleEndian)
                 _read = ReverseRead;
            else _read = DirectRead;
        }

        public byte SegmentSize { get; } = sizeof(ulong);
        public ulong LettersOut { get; } = ulong.MinValue;
        public ulong DigitsOut { get; } = ulong.MaxValue;
        
        public ulong ShiftLeft(ulong value, int bytesCount) =>
            value << (bytesCount * 8);

        public ulong ShiftRight(ulong value, int bytesCount) =>
            value >> (bytesCount * 8);

        public ulong Read(byte[] buff, int offset) =>
            _read(buff, offset);
        
        public ulong Merge(ulong a, ulong b) =>
            a | b;

        private static ulong DirectRead(byte[] buff, int offset) =>
            BitConverter.ToUInt64(buff, offset);
        
        private static ulong ReverseRead(byte[] buff, int offset) =>
            Reverse(DirectRead(buff, offset));
        
        private static ulong Reverse(ulong x) =>
             ((x & 0xFF00000000000000) >> 56)
           | ((x & 0x00FF000000000000) >> 40)
           | ((x & 0x0000FF0000000000) >> 24)
           | ((x & 0x000000FF00000000) >> 8)
           | ((x & 0x00000000FF000000) << 8)
           | ((x & 0x0000000000FF0000) << 24)
           | ((x & 0x000000000000FF00) << 40)
           | ((x & 0x00000000000000FF) << 56);
    }
}
