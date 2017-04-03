using System;

namespace Bigsort.Contracts
{
    public interface IGroupBytesMatrix
        : IGroupBytesMatrixInfo
        , IFixedSizeList<byte>
        , IDisposable
    {
        byte[][] Rows { get; }
    }
}
