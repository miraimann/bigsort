using System;

namespace Bigsort.Contracts
{
    internal interface ICacheableAccumulator<T>
        : IAccumulator<T>
        , IDisposable
    {
    }
}
