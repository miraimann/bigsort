using System;

namespace Bigsort.Contracts
{
    public interface IGroupMatrix
        : IFixedSizeList<byte>
        , IDisposable
    {
        byte[][] Rows { get; }
        int RowLength { get; }
        int RowsCount { get; }
        int LinesCount { get; }
        int BytesCount { get; }
    }
}
