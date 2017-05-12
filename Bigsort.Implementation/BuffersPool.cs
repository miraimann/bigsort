using System.Collections.Concurrent;
using System.Threading;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class BuffersPool
        : IBuffersPool
    {
        private readonly int _physicalBufferLength;
        private ConcurrentBag<byte[]> _storage;
        
        public BuffersPool(IConfig config)
        {
            _physicalBufferLength = config.PhysicalBufferLength;
            _storage = new ConcurrentBag<byte[]>();
        }

        public int Count =>
            _storage.Count;

        public byte[] Create() =>
            new byte[_physicalBufferLength];

        public Handle<byte[]> Get() =>
            TryGet() ?? Handle(new byte[_physicalBufferLength]);

        public Handle<byte[]> TryGet()
        {
            byte[] buffer;
            return _storage.TryTake(out buffer)
                ? Handle(buffer)
                : null;
        }

        public byte[][] ExtractAll()
        {
            var oldStorage = _storage;
            Interlocked.Exchange(ref _storage, new ConcurrentBag<byte[]>());
            return oldStorage.ToArray();
        }

        private Handle<byte[]> Handle(byte[] buffer) =>
            Handle<byte[]>.Make(buffer, buff => _storage.Add(buff));
        //, _storage.Add); is incorrect because _storage isn't readonly 
    }
}
