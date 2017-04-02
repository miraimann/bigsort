using System.Runtime.InteropServices;

namespace Bigsort.Contracts
{
    [StructLayout(LayoutKind.Explicit,
        Size = sizeof(int) + sizeof(byte) * 3 + sizeof(bool))]
    public struct LineIndexes
    {
        [FieldOffset(0)]
        public int start;

        [FieldOffset(4)]
        public byte digitsCount;

        [FieldOffset(5)]
        public byte lettersCount;

        [FieldOffset(6)]
        public byte sortingOffset;

        [FieldOffset(7)]
        public bool sortByDigits;
    }
}
