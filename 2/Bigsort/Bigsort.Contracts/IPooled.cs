using System;

namespace Bigsort.Contracts
{
    public interface IPooled<out T>
        : IDisposable
    {
        T Value { get; }
    }
}
