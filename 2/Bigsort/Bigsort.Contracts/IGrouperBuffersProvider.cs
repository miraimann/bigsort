using System;

namespace Bigsort.Contracts
{
    public interface IGrouperBuffersProvider
        : IDisposable
    {
        int TryGetNext(out IUsingHandle<byte[]> buffHandle);
    }
}
