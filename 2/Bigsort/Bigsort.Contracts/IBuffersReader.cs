using System;

namespace Bigsort.Contracts
{
    public interface IBuffersReader
        : IDisposable
    {
        int ReadNext(out IUsingHandle<byte[]> buffHandle);
    }
}
