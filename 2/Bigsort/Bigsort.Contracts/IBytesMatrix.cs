using System;
using System.Collections.Generic;

namespace Bigsort.Contracts
{
    public interface IBytesMatrix
        : IDisposable
    {
        byte[][] Content { get; }
        int RowsCount { get; }
        int RowLength { get; }
        int Count { get; }

        IReadOnlyList<byte> AsReadOnlyList();
    }
}
