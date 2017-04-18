using System;
using System.Threading;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class UsingHandleMaker
        : IUsingHandleMaker
    {
        public IUsingHandle<T> Make<T>(T value, Action<T> dispose) =>
            new Using<T>(value, dispose);

        public IMultiUsingHandle<T> MakeForMultiUse<T>(T value, Action<T> dispose) =>
            new MultiUsing<T>(value, dispose, this);

        private class MultiUsing<T>
            : IMultiUsingHandle<T>
        {
            private readonly Action<T> _dispose;
            private readonly IUsingHandleMaker _subUsingsMaker;
            private int _subUsersCount;

            public MultiUsing(T value, Action<T> dispose, 
                IUsingHandleMaker subUsingsMaker)
            {

                Value = value;
                _dispose = dispose;
                _subUsingsMaker = subUsingsMaker;
                Interlocked.Increment(ref _subUsersCount);
            }

            public T Value { get; }
            
            public IUsingHandle<T> SubUse()
            {
                Interlocked.Increment(ref _subUsersCount);
                return _subUsingsMaker.MakeForMultiUse(Value, x =>
                {
                    if (Interlocked.Decrement(ref _subUsersCount) == 0)
                        _dispose(Value);
                });
            }

            public void Dispose()
            {
                if (Interlocked.Decrement(ref _subUsersCount) == 0)
                    _dispose(Value);
            }
        }

        private struct Using<T>
            : IUsingHandle<T>
        {
            private readonly Action<T> _dispose;
            public Using(T value, Action<T> dispose)
            {
                Value = value;
                _dispose = dispose;
            }

            public T Value { get; }

            public void Dispose() =>
                _dispose(Value);
        }
    }
}
