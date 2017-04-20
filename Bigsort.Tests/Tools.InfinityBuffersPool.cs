using Bigsort.Contracts;

namespace Bigsort.Tests
{
    public static partial class Tools
    {
        public class InfinityBuffersPool
            : IBuffersPool
        {
            private readonly int _bufferSize;

            public InfinityBuffersPool(int bufferSize)
            {
                _bufferSize = bufferSize;
            }

            public int Count =>
                int.MaxValue;

            public IUsingHandle<byte[]> GetBuffer() =>
                new ZeroHandle<byte[]>(new byte[_bufferSize]);

            public IUsingHandle<byte[][]> TryGetBuffers(int count)
            {
                var buffers = new byte[count][];
                for (int i = 0; i < count; i++)
                    buffers[i] = new byte[_bufferSize];

                return new ZeroHandle<byte[][]>(buffers);
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
