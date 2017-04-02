using System;

namespace Bigsort.Contracts
{
    public interface IDisposableValueMaker
    {
        IDisposableValue<T> Make<T>(T value, Action<T> dispose);
    }
}
