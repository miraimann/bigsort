using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Bigsort.Contracts
{
    [StructLayout(LayoutKind.Explicit,
        Size = sizeof(int) + sizeof(uint))]
    public struct SortingLine
    {
        [FieldOffset(0)]
        public int start;

        [FieldOffset(sizeof(int))]
        public uint fragmentForSort;
        
        public class Comparer
            : IComparer<SortingLine>
        {
            private readonly IComparer<uint> _fragmentsComparer =
                Comparer<uint>.Default;

            public int Compare(SortingLine x, SortingLine y) =>
                _fragmentsComparer.Compare(
                    x.fragmentForSort,
                    y.fragmentForSort);
        }
    }
}
