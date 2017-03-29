using Bigsort.Contracts;
using System;
using System.Collections.Generic;

namespace Bigsort.Implementation
{
    public class PoolMaker
        : IPoolMaker
    {
        public IPool<T> Make<T>(Func<T> create, Action<T> clear) =>
            new Pool<T>(create, clear);

        public IPool<T> Make<T>(Func<T> create) =>
            new Pool<T>(create, _ => { /* do nothing */ });

        private class Pool<T>
            : IPool<T>
        {
            private readonly Func<T> _create;
            private readonly Action<T> _clear;

            private readonly Queue<T> _storage =
                new Queue<T>();

            public Pool(Func<T> create, Action<T> clear)
            {
                _create = create;
                _clear = clear;
            }

            public IPooled<T> Get()
            {
                if (_storage.Count == 0)
                {
                    _storage.Enqueue(_create());
                    return Get();
                }

                var product = _storage.Dequeue();
                return new Pooled(product, () =>
                {
                    _clear(product);
                    _storage.Enqueue(product);
                });
            }

            private class Pooled
                : IPooled<T>
            {
                private readonly Action _free; 
                public Pooled(T value, Action free)
                {
                    Value = value;
                    _free = free;
                }

                public T Value { get; }

                public void Dispose() =>
                    _free();
            }
        }
    }
}
