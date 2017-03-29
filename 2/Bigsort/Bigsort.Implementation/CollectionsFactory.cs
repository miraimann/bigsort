using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class CollectionsFactory
        : ICollectionsFactory
    {
        public IFragmentedList<T> CreateFragmentedList<T>() 
            where T : struct
        {
            throw new NotImplementedException();
        }

        private class FragmentedList<T>
            : IFragmentedList<T> 
            where T : struct
        {
            private readonly int _fragmentLength;
            private readonly IPool<T[]> _arraysPool;
            private readonly IList<IPooled<T[]>> _fragments;
            private int _latestFragmentLength;

            public FragmentedList(
                IPool<T[]> arraysPool,
                int fragmentLength)
            {
                _arraysPool = arraysPool;
                _fragmentLength = fragmentLength;
                _fragments = new List<IPooled<T[]>>();
            }

            public int Count =>
                Math.Max(0, _fragments.Count - 1)
                    * _fragmentLength
                    + _latestFragmentLength;

            public T this[int i] =>
                _fragments[i / _fragmentLength]
                    .Value[i % _fragmentLength];

            public void Add(T item)
            {
                if (_fragments.Count == 0 ||
                    _latestFragmentLength == _fragmentLength)
                {
                    _fragments.Add(_arraysPool.Get());
                    _latestFragmentLength = 0;
                }

                _fragments[_fragments.Count - 1]
                    .Value[_latestFragmentLength++] = item;
            }

            public IEnumerator<T> GetEnumerator()
            {
                if (_fragments.Count == 0)
                    yield break;

                for (int i = 0; i < _fragments.Count - 1; i++)
                    foreach (var item in _fragments[i].Value)
                        yield return item;

                var latestFragment = _fragments[_fragments.Count - 1];
                for (int i = 0; i < _latestFragmentLength; i++)
                    yield return latestFragment.Value[i];
            }

            IEnumerator IEnumerable.GetEnumerator() =>
                GetEnumerator();

            public void Dispose()
            {
                _latestFragmentLength = 0;
                foreach (var fragment in _fragments)
                    fragment.Dispose();
                _fragments.Clear();
            }
        }
    }
}
