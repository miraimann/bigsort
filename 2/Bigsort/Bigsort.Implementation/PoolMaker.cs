using System;
using System.Collections.Concurrent;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class PoolMaker
        : IPoolMaker
    {
        private readonly IUsingHandleMaker _usingHandleMaker;

        public PoolMaker(IUsingHandleMaker usingHandleMaker)
        {
            _usingHandleMaker = usingHandleMaker;
        }
        
        public IPool<T> Make<T>(Func<T> productFactory, 
                                Action<T> productCleaner = null, 
                                Action<T> productDestructor = null) =>
            new Pool<T>(
                _usingHandleMaker,
                productFactory,
                productCleaner,
                productDestructor);

        private class Pool<T>
            : IPool<T>
        {
            private readonly Func<T> _createProduct;
            private readonly Action<T> _clearProduct;
            private readonly Action<T> _destructProduct;
            private bool _disposed = false;

            private readonly IUsingHandleMaker _usingHandleMaker;
            private readonly ConcurrentQueue<T> _storage =
                new ConcurrentQueue<T>();

            public Pool(
                IUsingHandleMaker usingHandleMaker, 
                Func<T> productFactory, 
                Action<T> productCleaner, 
                Action<T> productDestructor)
            {
                _usingHandleMaker = usingHandleMaker;
                _createProduct = productFactory;
                _clearProduct = productCleaner;
                _destructProduct = productDestructor;
            }

            public IUsingHandle<T> Get()
            {
                IUsingHandle<T> handle;
                if (TryGet(out handle))
                    return handle;

                _storage.Enqueue(_createProduct());
                return Get();
            }

            public bool TryGet(out IUsingHandle<T> productHandle)
            {
                T product;
                if (_storage.TryDequeue(out product))
                {
                    productHandle = _usingHandleMaker.Make(product, x =>
                    {
                        if (_disposed) _destructProduct?.Invoke(x);
                        else
                        {
                            _clearProduct?.Invoke(x);
                            _storage.Enqueue(x);
                        }
                    });

                    return true;
                }

                productHandle = null;
                return false;
            }

            public bool TryExtract(out T product)
            {
                IUsingHandle<T> handle;
                if (TryGet(out handle))
                {
                    product = handle.Value;
                    return true;
                }

                product = default(T);
                return false;
            }

            public void Dispose()
            {
                _disposed = true;
                if (_destructProduct == null) return;
                foreach (var x in _storage)
                    _destructProduct(x);
            }
        }
    }
}
