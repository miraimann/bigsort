using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bigsort.Contracts
{
    public interface IFragmentedList<T>  
        : IReadOnlyList<T>
        , IDisposable
        where T : struct
    {
        void Add(T item);
    }
}
