using System;
using System.Collections.Generic;

namespace Bigsort.Contracts
{
    internal interface IAccumulator<T>
        : IReadOnlyList<T>
    {
        void Add(T item);
        void Clear();
    }
}
