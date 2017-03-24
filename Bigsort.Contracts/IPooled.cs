namespace Bigsort.Contracts
{
    internal interface IPooled<out T>
    {
        T Value { get; }
        void Free();
    }
}
