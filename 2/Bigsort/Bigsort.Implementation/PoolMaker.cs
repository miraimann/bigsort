using Bigsort.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Bigsort.Implementation
{
    public class PoolMaker
        : IPoolMaker
    {
        public IPool<T> MakePool<T>(Func<T> create, Action<T> clear) =>
            new Pool<T>(create, clear);

        public IPool<T> MakePool<T>(Func<T> create) =>
            new Pool<T>(create, _ => { /* do nothing */ });

        public IFragmentsPool<T> MakeFragmentsPool<T>(T[] resource) =>
            new FragmentsPool<T>(resource);
        
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
                return new Pooled<T>(product, () =>
                {
                    _clear(product);
                    _storage.Enqueue(product);
                });
            }
        }

        private class FragmentsPool<T>
            : IFragmentsPool<T>
        {
            private readonly LinkedList<int> _free;
            private readonly T[] _resource;
            
            public FragmentsPool(T[] resource)
            {
                _resource = resource;
                _free = new LinkedList<int>();
                var first = _free.AddFirst(0);
                _free.AddAfter(first, _resource.Length);
            }
            
            [SuppressMessage("ReSharper", 
                "PossibleNullReferenceException")]
            public IPooled<ArrayFragment<T>> TryGet(int length)
            {
                const int notFound = -1;
                int offset = notFound;
                {
                    var offsetLink = _free.First;
                    while (offsetLink != null)
                    {
                        var lengthLink = offsetLink.Next;
                        if (lengthLink.Value >= length)
                        {
                            offset = offsetLink.Value;
                            offsetLink.Value += length;
                            lengthLink.Value -= length;

                            if (lengthLink.Value == 0)
                            {
                                _free.Remove(lengthLink);
                                _free.Remove(offsetLink);
                            }

                            break;
                        }

                        offsetLink = lengthLink.Next;
                    }
                }

                if (offset == notFound)
                    return null;

                return new Pooled<ArrayFragment<T>>(
                    new ArrayFragment<T>(_resource, offset, length),
                    () =>
                    {
                        LinkedListNode<int>
                            offsetLink = _free.First,
                            prevOffsetLink = null;

                        while (offsetLink != null)
                        {
                            if (offsetLink.Value > offset)
                            {
                                var lengthLink = offsetLink.Next;
                                if (offset + length == offsetLink.Value)
                                {
                                    if (prevOffsetLink != null)
                                    {
                                        var prevLengthLink = prevOffsetLink.Next;
                                        if (prevOffsetLink.Value +
                                            prevLengthLink.Value == offset)
                                        {
                                            prevLengthLink.Value += length;
                                            prevLengthLink.Value += lengthLink.Value;

                                            _free.Remove(lengthLink);
                                            _free.Remove(offsetLink);
                                        }
                                    }
                                    else
                                    {
                                        lengthLink.Value += length;
                                        offsetLink.Value -= length;
                                    }
                                }
                                else
                                {
                                    var prevLengthLink = prevOffsetLink?.Next;
                                    if (prevOffsetLink != null &&
                                        prevOffsetLink.Value +
                                        prevLengthLink.Value == offset)
                                        prevLengthLink.Value += length;
                                    else
                                    {
                                        var newLengthLink = _free.AddBefore(offsetLink, length);
                                        _free.AddBefore(newLengthLink, offset);
                                    }
                                }

                                return;
                            }

                            prevOffsetLink = offsetLink;
                            offsetLink = offsetLink.Next.Next;
                        }

                        _free.AddLast(offset);
                        _free.AddLast(length);
                    }); 
            }
        }

        private class Pooled<T>
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
