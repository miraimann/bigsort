using System;

namespace Bigsort.Contracts
{
    public interface IGroupBytes
        : IGroupInfo
        , IFixedSizeList<byte>
        , IDisposable
    {
        byte[][] Rows { get; }
    }
}
