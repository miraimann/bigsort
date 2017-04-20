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
            private readonly Func<T, IUsingHandle<T>> _handle;
            private readonly Func<T[], IUsingHandle<T[]>> _handleRange;

            private readonly Func<T> _createProduct;
            private readonly Action<T> _destructProduct;
            private bool _disposed = false;
            
            private readonly ConcurrentStack<T> _storage = 
                new ConcurrentStack<T>();

            public Pool(
                IUsingHandleMaker handleMaker, 
                Func<T> productFactory, 
                Action<T> productCleaner, 
                Action<T> productDestructor)
            {
                _createProduct = productFactory;
                _destructProduct = productDestructor;

                var clear = productCleaner;
                var clearAndPush = clear + _storage.Push;
                var clearAndPushRange = productCleaner == null
                    ? new Action<T[]>(_storage.PushRange)
                    : products =>
                    {
                        foreach (var x in products) clear(x);
                        _storage.PushRange(products);
                    };

                if (productDestructor == null)
                {
                    _handle = product => handleMaker.Make(product, clearAndPush);
                    _handleRange = products => handleMaker.Make(products, clearAndPushRange);
                }
                else
                {
                    _handle = product => handleMaker.Make(product, x =>
                        (_disposed
                                ? _destructProduct
                                : clearAndPush)
                            (x));

                    Action<T[]> destructRange = products =>
                    {
                        foreach (var x in products)
                            _destructProduct(x);
                    };

                    _handleRange = products => handleMaker.Make(products, x =>
                        (_disposed
                                ? destructRange
                                : clearAndPushRange)
                            (x));
                }
            }

            public int Count =>
                _storage.Count;

            public IUsingHandle<T> Get()
            {
                IUsingHandle<T> handle;
                if (TryGet(out handle))
                    return handle;
                
                _storage.Push(_createProduct());
                return Get();
            }

            public bool TryGet(out IUsingHandle<T> productHandle)
            {
                T product;
                var success = _storage.TryPop(out product);
                productHandle = success
                    ? _handle(product)
                    : null;

                return success;
            }

            public bool TryExtract(out T product)
            {
                IUsingHandle<T> handle;
                var success = TryGet(out handle);
                product = success 
                    ? handle.Value 
                    : default(T);

                return success;
            }

            public IUsingHandle<T[]> GetRange(int count)
            {
                throw new NotImplementedException();
            }

            public bool TryGetRange(int count, out IUsingHandle<T[]> productsHandle)
            {
                if (Count >= count)
                {
                    T[] products = new T[count];
                    var poppedCount = _storage.TryPopRange(products);
                    if (poppedCount == count)
                    {
                        productsHandle = _handleRange(products);
                        return true;
                    }

                    if (poppedCount != 0)
                        _storage.PushRange(products, 0, poppedCount);
                }

                productsHandle = null;
                return false;
            }

            public bool TryExtractRange(int count, out T[] products)
            {
                throw new NotImplementedException();
            }

            public void Free(int count)
            {
                T[] linkOut = new T[count];
                count = _storage.TryPopRange(linkOut);
                if (_destructProduct != null)
                    for (int i = 0; i < count; i++)
                        _destructProduct(linkOut[i]);
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
