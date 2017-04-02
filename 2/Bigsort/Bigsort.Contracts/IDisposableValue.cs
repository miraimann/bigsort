using System;

namespace Bigsort.Contracts
{
    public interface IDisposableValue<out T>
        : IDisposable
    {
        T Value { get; }
    }
}
