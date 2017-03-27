using System;

namespace Bigsort.Contracts
{
    internal interface IPoolMaker
    {
        IPool<T> Make<T>(Func<T> create, Action<T> clear);
        IPool<T> Make<T>(Func<T> create);
    }
}
