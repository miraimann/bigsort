using System;

namespace Bigsort.Contracts
{
    public class Handle<T>
        : IDisposable
    {
        private readonly Action<T> _dispose;
        private Handle(T value, Action<T> dispose)
        {
            Value = value;
            _dispose = dispose;
        }

        public T Value { get; }

        public void Dispose() =>
            _dispose(Value);

        public static Handle<T> Make(T value, Action<T> dispose) =>
            new Handle<T>(value, dispose);
        
        public static Handle<T> Adapt(T value) =>
            new Handle<T>(value, _ => { });

        public static Handle<T> Zero { get; } =
            Adapt(default(T));
    }
}
