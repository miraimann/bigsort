using System;

namespace Bigsort.Contracts
{
    public interface IAsyncReader
        : IDisposable
    {
        int Read(out IUsingHandle<byte[]> handle);
    }
}
