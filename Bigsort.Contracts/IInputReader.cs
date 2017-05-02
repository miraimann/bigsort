using System;

namespace Bigsort.Contracts
{
    public interface IInputReader
        : IDisposable
    {
        int GetFirstBuffer(out IUsingHandle<byte[]> buffHandle);
        int TryGetNextBuffer(out IUsingHandle<byte[]> buffHandle);
    }
}
