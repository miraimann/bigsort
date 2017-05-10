using System;
using System.Collections.Concurrent;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class PoolMaker
        : IPoolMaker
    {
        public IPool<T> MakePool<T>(
                Func<T> productFactory,
                Action<T> productCleaner = null) =>
            
            new Pool<T>(
                productFactory,
                productCleaner);

        public IDisposablePool<T> MakeDisposablePool<T>(
            Func<T> productFactory,
            Action<T> productCleaner = null)
            where T : IDisposable =>

            new DisposablePool<T>(
                productFactory,
                productCleaner);

        private class BasePool<TCollection, T> : IPool<T>
            where TCollection : IProducerConsumerCollection<T>, new()
        {
            private readonly Action<TCollection, T> _returnProduct;
            private readonly Func<T> _createProduct;
            protected TCollection Storage;

            protected BasePool(
                TCollection storage,
                Func<T> createProduct,
                Action<TCollection, T> returnProduct)
            {
                _returnProduct = returnProduct;
                _createProduct = createProduct;
                Storage = storage;
            }

            public int Count =>
                Storage.Count;

            public Handle<T> Get() =>
                TryGet() ?? Handle(_createProduct());

            public Handle<T> TryGet()
            {
                T product;
                return Storage.TryTake(out product)
                    ? Handle(product)
                    : null;
            }

            public T TryExtract()
            {
                T product;
                return Storage.TryTake(out product)
                    ? product
                    : default(T);
            }

            public T[] ExtractAll()
            {
                var oldStorage = Storage;
                Storage = new TCollection();
                return oldStorage.ToArray();
            }

            protected virtual void ReturnProduct(T x) =>
                _returnProduct(Storage, x);

            private Handle<T> Handle(T product) =>
                Handle<T>.Make(product, ReturnProduct);
        }

        private class Pool<T>
            : BasePool<ConcurrentBag<T>,  T>
        {
            public Pool(Func<T> createProduct,
                        Action<T> clearProduct)
                
                : this(new ConcurrentBag<T>(),
                       createProduct,
                       clearProduct)
            {
            }

            private Pool(ConcurrentBag<T> storage,
                         Func<T> createProduct,
                         Action<T> clearProduct)

                : base(storage,
                       createProduct,
                       clearProduct == null
                            ? new Action<ConcurrentBag<T>, T>(
                              (s, x) => s.Add(x))
                            : (s, x) =>
                            {
                                clearProduct(x);
                                s.Add(x);
                            })
            {
            }
        }

        private class DisposablePool<T>
            : Pool<T>
            , IDisposablePool<T>
            where T : IDisposable
        {
            private volatile Action<T> _returnProduct;

            public DisposablePool(Func<T> createProduct, 
                                  Action<T> clearProduct) 
                : base(createProduct,
                       clearProduct)
            {
                _returnProduct = base.ReturnProduct;
            }

            public void Dispose()
            {
                _returnProduct = product => product.Dispose();
                foreach (var x in Storage)
                    x.Dispose();
            }

            protected override void ReturnProduct(T x) =>
                _returnProduct(x);
        }
    }
}
