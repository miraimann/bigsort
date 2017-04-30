using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        public IRangesPool MakRangesPool(int length) =>
            new RangesPool(length, _usingHandleMaker);

        public IRangablePool<T> MakeRangablePool<T>(
                Func<T> productFactory,
                Action<T> productCleaner = null) =>

            MakeRangablePool(
                Enumerable.Empty<T>(),
                productFactory,
                productCleaner);

        public IRangablePool<T> MakeRangablePool<T>(
                IEnumerable<T> init,
                Func<T> productFactory,
                Action<T> productCleaner = null) =>

            new RangablePool<T>(
                init,
                productFactory,
                productCleaner,
                _usingHandleMaker);

        private class BasePool<TCollection, T> : IPool<T>
            where TCollection : IProducerConsumerCollection<T>, new()
        {
            private readonly Action<TCollection, T> _returnProduct;
            protected readonly IUsingHandleMaker HandleMaker;
            protected readonly Func<T> CreateProduct;
            protected TCollection Storage;

            protected BasePool(
                TCollection storage,
                Func<T> createProduct,
                Action<TCollection, T> returnProduct,
                IUsingHandleMaker handleMaker)
            {
                _returnProduct = returnProduct;
                CreateProduct = createProduct;
                HandleMaker = handleMaker;
                Storage = storage;
            }

            public int Count =>
                Storage.Count;

            public IUsingHandle<T> Get() =>
                TryGet() ?? Handle(CreateProduct());

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
                HandleMaker.Make(product, ReturnProduct);
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
        
        private class RangablePool<T>
            : BasePool<ConcurrentStack<T>, T>
            , IRangablePool<T>
        {
            private readonly Action<ConcurrentStack<T>, T[]> _returnProducts;
            
            public RangablePool(IEnumerable<T> init,
                                Func<T> createProduct,
                                Action<T> clearProduct,
                                IUsingHandleMaker handleMaker)
                
                : base(new ConcurrentStack<T>(init),
                       createProduct,
                       clearProduct == null
                            ? new Action<ConcurrentStack<T>, T>(
                              (storage, x) => storage.Push(x))
                            : (storage, x) =>
                            {
                                clearProduct(x);
                                storage.Push(x);
                            },
                       handleMaker)
            {
                _returnProducts = clearProduct == null
                    ? new Action<ConcurrentStack<T>, T[]>(
                      (storage, products) => storage.PushRange(products))
                    : (storage, products) =>
                    {
                        Array.ForEach(products, clearProduct);
                        Storage.PushRange(products);
                    };
            }
            
            public IUsingHandle<T[]> TryGet(int count)
            {
                if (Count >= count)
                {
                    T[] products = new T[count];
                    var poppedCount = Storage.TryPopRange(products);
                    if (poppedCount == count)
                        return HandleMaker.Make(products, Return);

                    if (poppedCount != 0)
                        Storage.PushRange(products, 0, poppedCount);
                }

                return null;
            }

            public IUsingHandle<T[]> Get(int count)
            {
                T[] products = new T[count];
                var poppedCount = Storage.TryPopRange(products);
                while (poppedCount != count)
                    products[poppedCount++] = CreateProduct();

                return HandleMaker.Make(products, Return);
            }

            public T[] TryExtract(int count)
            {
                if (Count >= count)
                {
                    T[] products = new T[count];
                    var poppedCount = Storage.TryPopRange(products);
                    if (poppedCount != count)
                        Storage.PushRange(products, 0, poppedCount);
                    else return products;
                }

                return null;
            }

            private void Return(T[] products) =>
                _returnProducts(Storage, products);
        }

        private class LazyRangablePool<T>
            : IRangablePool<T>
        {
            public int Count { get; }
            public IUsingHandle<T> Get()
            {
                throw new NotImplementedException();
            }

            public IUsingHandle<T> TryGet()
            {
                throw new NotImplementedException();
            }

            public T TryExtract()
            {
                throw new NotImplementedException();
            }

            public T[] ExtractAll()
            {
                throw new NotImplementedException();
            }

            public IUsingHandle<T[]> TryGet(int count)
            {
                throw new NotImplementedException();
            }

            public IUsingHandle<T[]> Get(int count)
            {
                throw new NotImplementedException();
            }

            public T[] TryExtract(int count)
            {
                throw new NotImplementedException();
            }
        }

        private class RangesPool
            : IRangesPool
        {
            private readonly IUsingHandleMaker _usingHandleMaker;
            private readonly object o = new object();
            private readonly LinkedList<Range> _free =
                new LinkedList<Range>();
            
            public RangesPool(int length, IUsingHandleMaker usingHandleMaker)
            {
                Length = length;
                _free.AddFirst(new Range(0, Length));
                _usingHandleMaker = usingHandleMaker;
            }

            public int Length { get; }

            public IUsingHandle<Range> TryGet(int length)
            {
                const int notFound = -1;
                int offset = notFound;
                lock (o)
                {
                    var link = _free.First;
                    while (link != null)
                    {
                        if (link.Value.Length >= length)
                        {
                            offset = link.Value.Offset;
                            var cutedLength = link.Value.Length - length;
                            if (cutedLength == 0)
                                _free.Remove(link);
                            else
                                link.Value = new Range(
                                    link.Value.Offset + length,
                                    cutedLength);
                            break;
                        }

                        link = link.Next;
                    }
                }

                return offset == notFound
                    ? null
                    : _usingHandleMaker.Make(
                        new Range(offset, length),
                        delegate
                        {
                            lock (o)
                            {
                                LinkedListNode<Range>
                                    link = _free.First,
                                    prev = null;

                                while (link != null)
                                {
                                    if (link.Value.Offset > offset)
                                    {
                                        if (offset + length == link.Value.Offset)
                                        {
                                            if (prev != null &&
                                                prev.Value.Offset +
                                                prev.Value.Length == offset)
                                            {
                                                var newLength = prev.Value.Length
                                                                + link.Value.Length
                                                                + length;

                                                prev.Value = new Range(
                                                    prev.Value.Offset,
                                                    newLength);

                                                _free.Remove(link);
                                            }
                                            else
                                                link.Value = new Range(
                                                    link.Value.Offset - length,
                                                    link.Value.Length + length);
                                        }
                                        else
                                        {
                                            if (prev != null &&
                                                prev.Value.Offset +
                                                prev.Value.Length == offset)
                                                prev.Value = new Range(
                                                    prev.Value.Offset,
                                                    prev.Value.Length + length);
                                            else
                                                _free.AddBefore(link,
                                                    new Range(offset, length));
                                        }

                                        return;
                                    }

                                    prev = link;
                                    link = link.Next;
                                }

                                _free.AddLast(new Range(offset, length));
                            }
                        });
            }
        }
    }
}
