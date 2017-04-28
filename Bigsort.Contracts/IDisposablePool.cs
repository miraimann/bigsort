using System;

namespace Bigsort.Contracts
{
    public interface IDisposablePool<T>
        : IPool<T>
        , IDisposable
    {
    }
}
