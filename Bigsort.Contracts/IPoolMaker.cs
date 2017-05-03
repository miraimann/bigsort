using System;
using System.Collections.Generic;

namespace Bigsort.Contracts
{
    public interface IPoolMaker
    {
        IPool<T> MakePool<T>(
            Func<T> productFactory,
            Action<T> productCleaner = null);

        IDisposablePool<T> MakeDisposablePool<T>(
            Func<T> productFactory,
            Action<T> productCleaner = null)
            where T : IDisposable;
    }
}
