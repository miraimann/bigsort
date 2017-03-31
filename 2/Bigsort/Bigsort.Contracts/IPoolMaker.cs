using System;

namespace Bigsort.Contracts
{
    public interface IPoolMaker
    {
        IPool<T> MakePool<T>(Func<T> create, Action<T> clear);
        IPool<T> MakePool<T>(Func<T> create);

        IFragmentsPool<T> MakeFragmentsPool<T>(T[] resource);
    }
}
