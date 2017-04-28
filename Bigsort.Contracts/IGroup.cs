using System;

namespace Bigsort.Contracts
{
    public interface IGroup
        : IFixedSizeList<byte>
        , IDisposable
    {
        byte[][] Buffers { get; }

        Range LinesRange { get; }
        
        int BuffersCount { get; }

        int LinesCount { get; }

        int BytesCount { get; }
    }
}
