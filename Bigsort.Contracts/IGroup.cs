using System;

namespace Bigsort.Contracts
{
    public interface IGroup
        : IFixedSizeList<byte>
        , IDisposable
    {
        byte[][] Buffers { get; }
        
        ArraySegment<LineIndexes> Lines { get; }

        ArraySegment<ulong> SortingSegments { get; }
        
        int BytesCount { get; }
    }
}
