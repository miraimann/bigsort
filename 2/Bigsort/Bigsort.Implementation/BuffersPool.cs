using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class BuffersPool
        : IBuffersPool
    {
        private readonly IPool<byte[]> _pool;

        public BuffersPool(
            IPoolMaker poolMaker,
            IConfig config)
        {
            _pool = poolMaker.MakePool(() =>
                    new byte[config.BufferSize]);
        }

        public IPooled<byte[]> Get() =>
            _pool.Get();
    }
}
