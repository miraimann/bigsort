using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class BuffersPool
        : IBuffersPool
    {
        private readonly IPool<byte[]> _implementation;
        
        public BuffersPool(IPoolMaker poolMaker, IConfig config)
        {
            _implementation = poolMaker.Make(
                productFactory: () => new byte[config.BufferSize]);
        }

        public IUsingHandle<byte[]> GetBuffer() =>
            _implementation.Get();
    }
}
