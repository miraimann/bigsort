using System;

namespace Bigsort2.Contracts
{
    public interface IReader
        : IDisposable
    {
        long Position { get; }

        byte NextByte();
        ushort NextUInt16();
        ulong NextUInt64();
    }
}
