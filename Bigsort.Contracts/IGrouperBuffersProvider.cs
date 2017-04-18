using System;

namespace Bigsort.Contracts
{
    public interface IGrouperBuffersProvider
        : IDisposable
    {
        int TryGetNextBuffer(out IUsingHandle<byte[]> buffHandle);
    }
}
