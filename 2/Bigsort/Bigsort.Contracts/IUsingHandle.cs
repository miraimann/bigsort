using System;

namespace Bigsort.Contracts
{
    public interface IUsingHandle<out T>
        : IDisposable
    {
        T Value { get; }
    }
}
