using System;
using System.Threading.Tasks;

namespace Bigsort.Contracts
{
    public interface IAsyncReader
        : IDisposable
    {
        // Task<Buffff> Read();
        int Read(out IUsingHandle<byte[]> handle);
    }
}
