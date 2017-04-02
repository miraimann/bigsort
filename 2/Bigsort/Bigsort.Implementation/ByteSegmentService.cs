using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class ByteSegmentService
        : ISegmentService<byte>
    {
        public byte SegmentSize { get; } = sizeof(byte);
        public byte LettersOut { get; } = byte.MinValue;
        public byte DigitsOut { get; } = byte.MaxValue;

        public byte ShiftLeft(byte value, int bytesCount) =>
            bytesCount == 0 ? value : (byte)0;

        public byte ShiftRight(byte value, int bytesCount) =>
            bytesCount == 0 ? value : (byte)0;

        public byte Read(byte[] buff, int offset) =>
            buff[offset];

        public byte Merge(byte a, byte b) =>
            (byte)(a | b);
    }
}
