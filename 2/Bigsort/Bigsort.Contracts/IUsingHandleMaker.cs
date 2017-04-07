using System;

namespace Bigsort.Contracts
{
    public interface IUsingHandleMaker
    {
        IUsingHandle<T> Make<T>(T value, Action<T> dispose);
        IMultiUsingHandle<T> MakeForMultiUse<T>(T value, Action<T> dispose);
    }
}
