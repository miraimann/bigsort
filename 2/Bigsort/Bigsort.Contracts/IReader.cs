using System;

namespace Bigsort.Contracts
{
    public interface IReader
        : IDisposable
    {
        long Position { get; set; }

        int Read(byte[] buff, int offset, int count);

        int ReadByte();
    }
}
