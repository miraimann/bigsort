using System;

namespace Bigsort.Contracts
{
    public interface IReader
        : IDisposable
    {
        int Read(byte[] buff, int offset, int count);
    }
}
