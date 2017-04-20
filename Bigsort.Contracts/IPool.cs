using System;

namespace Bigsort.Contracts
{
    public interface IPool<T>
        : IDisposable
    {
        int Count { get; }

        IUsingHandle<T> Get();

        bool TryGet(out IUsingHandle<T> productHandle);

        bool TryExtract(out T product);

        IUsingHandle<T[]> GetRange(int count);

        bool TryGetRange(int count, out IUsingHandle<T[]> productsHandle);

        bool TryExtractRange(int count, out T[] products);

        void Free(int count);
    }
}
