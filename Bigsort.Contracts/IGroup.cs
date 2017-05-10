using System;

namespace Bigsort.Contracts
{
    internal interface IGroup
    {
        ArraySegment<byte[]> Buffers { get; }
        ArraySegment<LineIndexes> Lines { get; }
        ArraySegment<ulong> SortingSegments { get; }
        
        int BytesCount { get; }
    }
}
