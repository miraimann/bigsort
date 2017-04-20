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

        public int Count =>
            _implementation.Count;

        public IUsingHandle<byte[]> GetBuffer() =>
            _implementation.Get();

        public IUsingHandle<byte[][]> TryGetBuffers(int count)
        {
            IUsingHandle<byte[][]> buffersHandle;
            if (_implementation.TryGetRange(count, out buffersHandle))
                return buffersHandle;
            return null;
        }

        public void Free(int count) =>
            _implementation.Free(count);
    }
}
