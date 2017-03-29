using System.Runtime.InteropServices;

namespace Bigsort.Contracts
{
    [StructLayout(LayoutKind.Explicit, 
        Size = sizeof(int) + sizeof(ushort) + sizeof(byte))]
    public struct LineIndexes
    {
        [FieldOffset(0)]
        public int start;

        [FieldOffset(sizeof(int))]
        public ushort lettersCount;

        [FieldOffset(sizeof(int) + sizeof(ushort))]
        public byte digitsCount;
    }
}
