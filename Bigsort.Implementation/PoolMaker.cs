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
        
        public IPool<T> MakePool<T>(
                Func<T> productFactory,
                Action<T> productCleaner = null) =>
            
            new Pool<T>(
                productFactory,
                productCleaner,
                _usingHandleMaker);

        public IDisposablePool<T> MakeDisposablePool<T>(
            Func<T> productFactory,
            Action<T> productCleaner = null)
            where T : IDisposable =>

            new DisposablePool<T>(
                productFactory,
                productCleaner,
                _usingHandleMaker);

        private class BasePool<TCollection, T> : IPool<T>
            where TCollection : IProducerConsumerCollection<T>, new()
        {
            private readonly Action<TCollection, T> _returnProduct;
            private readonly IUsingHandleMaker _handleMaker;
            private readonly Func<T> _createProduct;
            protected TCollection Storage;

            protected BasePool(
                TCollection storage,
                Func<T> createProduct,
                Action<TCollection, T> returnProduct,
                IUsingHandleMaker handleMaker)
            {
                _returnProduct = returnProduct;
                _createProduct = createProduct;
                _handleMaker = handleMaker;
                Storage = storage;
            }

            public int Count =>
                Storage.Count;

            public IUsingHandle<T> Get() =>
                TryGet() ?? Handle(_createProduct());

            public IUsingHandle<T> TryGet()
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

            private IUsingHandle<T> Handle(T product) =>
                _handleMaker.Make(product, ReturnProduct);
        }

        private class Pool<T>
            : BasePool<ConcurrentBag<T>,  T>
        {
            public Pool(Func<T> createProduct,
                        Action<T> clearProduct,
                        IUsingHandleMaker handleMaker)
                
                : this(new ConcurrentBag<T>(),
                       createProduct,
                       clearProduct,
                       handleMaker)
            {
            }

            private Pool(ConcurrentBag<T> storage,
                         Func<T> createProduct,
                         Action<T> clearProduct,
                         IUsingHandleMaker handleMaker)

                : base(storage,
                       createProduct,
                       clearProduct == null
                            ? new Action<ConcurrentBag<T>, T>(
                              (s, x) => s.Add(x))
                            : (s, x) =>
                            {
                                clearProduct(x);
                                s.Add(x);
                            },
                       handleMaker)
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
                                  Action<T> clearProduct,
                                  IUsingHandleMaker handleMaker) 
                : base(createProduct,
                       clearProduct,
                       handleMaker)
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
