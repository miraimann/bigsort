using System.Collections.Concurrent;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class BuffersPool
        : IBuffersPool
    {
        private readonly IDisposableValueMaker _disposableValueMaker;
        private readonly IConfig _config;

        private readonly ConcurrentQueue<byte[]> _storage =
            new ConcurrentQueue<byte[]>();

        public BuffersPool(
            IDisposableValueMaker disposableValueMaker,
            IConfig config)
        {
            _disposableValueMaker = disposableValueMaker;
            _config = config;
        }

        public IDisposableValue<byte[]> GetBuffer()
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
