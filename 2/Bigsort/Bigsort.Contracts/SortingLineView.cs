using System.Runtime.InteropServices;

namespace Bigsort.Contracts
{
    [StructLayout(LayoutKind.Explicit,
        Size = sizeof(int) + sizeof(uint))]
    public struct SortingLineView
    {
        [FieldOffset(0)]
        public int start;

        [FieldOffset(sizeof(int))]
        public uint partForSort;
    }
}
