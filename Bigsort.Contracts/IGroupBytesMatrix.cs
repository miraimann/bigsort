using System;

namespace Bigsort.Contracts
{
    public interface IGroupBytesMatrix
        : IGroupBytesMatrixRowsInfo
        , IFixedSizeList<byte>
        , IDisposable
    {
        byte[][] Rows { get; }
        int LinesCount { get; }
        int BytesCount { get; }
    }
}
