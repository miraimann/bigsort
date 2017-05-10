using System;

namespace Bigsort.Contracts
{
    internal interface IInputReader
        : IDisposable
    {
        int GetFirstBuffer(out Handle<byte[]> buffHandle);
        int TryGetNextBuffer(out Handle<byte[]> buffHandle);
    }
}
