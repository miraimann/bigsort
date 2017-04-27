using System;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class UsingHandleMaker
        : IUsingHandleMaker
    {
        public IUsingHandle<T> Make<T>(T value, Action<T> dispose) =>
            new Using<T>(value, dispose);

        private class Using<T>
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
