using System.Collections.Concurrent;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class BuffersPool
        : IBuffersPool
    {
        private readonly IUsingHandleMaker _disposableValueMaker;
        private readonly IConfig _config;

        private readonly ConcurrentQueue<byte[]> _storage =
            new ConcurrentQueue<byte[]>();

        public BuffersPool(
            IUsingHandleMaker disposableValueMaker,
            IConfig config)
        {
            _disposableValueMaker = disposableValueMaker;
            _config = config;
        }

        public IUsingHandle<byte[]> GetBuffer()
        {
            byte[] product;
            if (_storage.TryDequeue(out product))
                return _disposableValueMaker
                    .Make(product, _storage.Enqueue);

            _storage.Enqueue(new byte[_config.BufferSize]);
            return GetBuffer();
        }
    }
}
