using System;

namespace Bigsort.Contracts
{
    public interface IPool<T>
        : IDisposable
    {
        IUsingHandle<T> Get();

        bool TryGet(out IUsingHandle<T> productHandle);

        bool TryExtract(out T product);
    }
}
