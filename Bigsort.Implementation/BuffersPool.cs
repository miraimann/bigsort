using System;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class BuffersPool
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

        public IUsingHandle<byte[]> Get() =>
            _pool.Get();

        public IUsingHandle<byte[]> TryGet() =>
            _pool.TryGet();

        public byte[] TryExtract() =>
            _pool.TryExtract();

        public byte[][] ExtractAll() =>
            _pool.ExtractAll();
    }
}
