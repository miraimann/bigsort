using System;

namespace Bigsort.Contracts
{
    public interface IAsyncWriter
        : IDisposable
    {
        long Length { get; }

        void Write(IUsingHandle<byte[]> buffHandale, 
            int offset, int count);
    }
}
