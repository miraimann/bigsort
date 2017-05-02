using Bigsort.Contracts;

namespace Bigsort.Tests
{
    public static partial class Tools
    {
        public class InfinityBuffersPool
            : IBuffersPool
        {
            private const int DefaultMemoryLimit = 1024 * 1024 * 1024;
            private readonly int _bufferSize, _memoryLimit;

            public InfinityBuffersPool(int bufferSize, 
                int memoryLimit = DefaultMemoryLimit)
            {
                _bufferSize = bufferSize;
                _memoryLimit = memoryLimit;
            }

            public int Count =>
                _memoryLimit / _bufferSize + 1;

            public IUsingHandle<byte[]> Get() =>
                new ZeroHandle<byte[]>(new byte[_bufferSize]);

            public IUsingHandle<byte[]> TryGet() =>
                Get();

            public byte[] TryExtract() =>
                TryGet().Value;

            public byte[][] ExtractAll()
            {
                byte[][] all = new byte[Count][];
                for (int i = 0; i < Count; i++)
                    all[i] = new byte[_bufferSize];
                return all;
            }

            public void Free(int _) { }

            private class ZeroHandle<T>
                : IUsingHandle<T>
            {
                public ZeroHandle(T value)
                {
                    Value = value;
                }

                public void Dispose()
                {
                }

                public T Value { get; }
            }
        }
    }
}
