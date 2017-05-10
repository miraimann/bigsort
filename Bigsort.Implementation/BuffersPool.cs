using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class BuffersPool
        : IBuffersPool
    {
        private readonly int _physicalBufferLength;
        private readonly IPool<byte[]> _pool;
        
        public BuffersPool(
            IPoolMaker poolMaker,
            IConfig config)
        {
            _physicalBufferLength = config.PhysicalBufferLength;
            _pool = poolMaker.MakePool(Create);
        }

        public int Count =>
            _pool.Count;

        public byte[] Create() =>
            new byte[_physicalBufferLength];

        public Handle<byte[]> Get() =>
            _pool.Get();

        public Handle<byte[]> TryGet() =>
            _pool.TryGet();

        public byte[] TryExtract() =>
            _pool.TryExtract();

        public byte[][] ExtractAll() =>
            _pool.ExtractAll();
    }
}
