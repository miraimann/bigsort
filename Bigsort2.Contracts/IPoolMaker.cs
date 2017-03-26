using System;

namespace Bigsort2.Contracts
{
    public interface IPoolMaker
    {
        IPool<T> Make<T>(Func<T> create, Action<T> clear);
        IPool<T> Make<T>(Func<T> create);
    }
}
