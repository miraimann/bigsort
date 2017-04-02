using System;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class DisposableValueMaker
        : IDisposableValueMaker
    {
        public IDisposableValue<T> Make<T>(
                T value, Action<T> dispose) =>
            new DisposableValue<T>(value, dispose);

        private class DisposableValue<T>
            : IDisposableValue<T>
        {
            private readonly Action<T> _dispose;
            public DisposableValue(T value, Action<T> dispose)
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
