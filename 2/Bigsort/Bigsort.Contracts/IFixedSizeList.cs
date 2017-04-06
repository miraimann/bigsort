using System.Collections.Generic;

namespace Bigsort.Contracts
{
    public interface IFixedSizeList<T>
        : IReadOnlyList<T>
    {
        new T this[int i] { get; set; }
    }
}