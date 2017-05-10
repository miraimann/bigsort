using System;

namespace Bigsort.Contracts
{
    internal interface IDisposablePool<T>
        : IPool<T>
        , IDisposable
    {
    }
}
