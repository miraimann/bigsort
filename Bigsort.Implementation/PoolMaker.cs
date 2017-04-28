using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

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
                Action<T> productDestructor,
                Action<T> productCleaner = null) =>

            new DisposablePool<T>(
                productFactory,
                productDestructor,
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
        
        private abstract class BasePool<T>
            : IPool<T>
        {
            private readonly IUsingHandleMaker _handleMaker;
            private readonly Func<T> _createProduct;
            protected ConcurrentBag<T> Storage =
                new ConcurrentBag<T>();

            protected BasePool(Func<T> createProduct, 
                IUsingHandleMaker handleMaker)
            {
                _createProduct = createProduct;
                _handleMaker = handleMaker;
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
                var all = Storage.ToArray();
                Storage = new ConcurrentBag<T>();
                return all;
            }

            protected abstract void Return(T product);

            private IUsingHandle<T> Handle(T product) =>
                _handleMaker.Make(product, Return);
        }

        private class Pool<T>
            : BasePool<T>
        {
            protected readonly Action<T> ReturnProduct;

            public Pool(Func<T> createProduct,
                        Action<T> clearProduct,
                        IUsingHandleMaker handleMaker) 
                : base(createProduct, handleMaker)
            {
                ReturnProduct = clearProduct + Storage.Add;
            }

            protected override void Return(T x) =>
                ReturnProduct(x);
        }

        private class DisposablePool<T>
            : Pool<T>
            , IDisposablePool<T>
        {
            private volatile Action<T> _returnProduct;
            private readonly Action<T> _disposeProduct;

            public DisposablePool(
                Func<T> createProduct, 
                Action<T> clearProduct,
                Action<T> disposeProduct,
                IUsingHandleMaker handleMaker) 
                : base(createProduct, clearProduct, handleMaker)
            {
                _disposeProduct = disposeProduct;
                _returnProduct = ReturnProduct;
            }

            protected override void Return(T x) =>
                _returnProduct(x);

            public void Dispose()
            {
                _returnProduct = _disposeProduct;
                foreach (var x in Storage)
                    _disposeProduct(x);
            }
        }

        public class RangesPool
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

        private class RangablePool<T>
            : IRangablePool<T>
        {
            private readonly IUsingHandleMaker _usingHandleMaker;
            private readonly Func<T> _createProduct;
            private readonly Action<T[]> _returnProducts;
            private readonly ConcurrentStack<T> _storage;

            public RangablePool(
                IEnumerable<T> init,
                Func<T> productFactory,
                Action<T> productCleaner,
                IUsingHandleMaker handleMaker)
            {
                _storage = new ConcurrentStack<T>(init);
                _usingHandleMaker = handleMaker;
                _createProduct = productFactory;

                var clear = productCleaner != null
                    ? new Action<T[]>(products =>
                    {
                        for (int i = 0; i < products.Length; i++)
                            productCleaner(products[i]);
                    })
                    : null;
                
               _returnProducts = clear + _storage.PushRange;
            }

            public int Count =>
                _storage.Count;

            public IUsingHandle<T[]> TryGet(int count)
            {
                if (Count >= count)
                {
                    T[] products = new T[count];
                    var poppedCount = _storage.TryPopRange(products);
                    if (poppedCount == count)
                        return _usingHandleMaker.Make(products, _returnProducts);

                    if (poppedCount != 0)
                        _storage.PushRange(products, 0, poppedCount);
                }
                
                return null;
            }

            public IUsingHandle<T[]> Get(int count)
            {
                T[] products = new T[count];
                var poppedCount = _storage.TryPopRange(products);
                while (poppedCount != count)
                    products[poppedCount++] = _createProduct();
                
                return _usingHandleMaker.Make(products, _returnProducts);
            }

            public T[] TryExtract(int count)
            {
                if (Count >= count)
                {
                    T[] products = new T[count];
                    var poppedCount = _storage.TryPopRange(products);
                    if (poppedCount != count)
                        _storage.PushRange(products, 0, poppedCount);
                    else return products;
                }

                return null;
            }
        }
    }
}
