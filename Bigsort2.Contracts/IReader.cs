using System;

namespace Bigsort2.Contracts
{
    public interface IReader
        : IDisposable
    {
        int Read(byte[] buff, int offset, int count);
    }
}
