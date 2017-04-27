using System;

namespace Bigsort.Contracts
{
    public interface IGroup
        : IFixedSizeList<byte>
        , IDisposable
    {
        byte[][] Rows { get; }

        Range LinesRange { get; }

        int RowLength { get; }
        int RowsCount { get; }
        int LinesCount { get; }
        int BytesCount { get; }
    }
}
