using System;

namespace Bigsort.Contracts
{
    public interface IGroup
    {
        ArraySegment<byte[]> Buffers { get; }
        ArraySegment<LineIndexes> Lines { get; }
        ArraySegment<ulong> SortingSegments { get; }
        
        int BytesCount { get; }
    }
}
