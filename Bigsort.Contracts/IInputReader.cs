using System;

namespace Bigsort.Contracts
{
    public interface IInputReader
        : IDisposable
    {
        int TryGetNextBuffer(out IUsingHandle<byte[]> buffHandle);
    }
}
