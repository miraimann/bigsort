using System;

namespace Bigsort.Contracts
{
    public interface IPoolMaker
    {
        IPool<T> Make<T>(Func<T> productFactory,
                         Action<T> productCleaner = null,
                         Action<T> productDestructor = null);
    }
}
