namespace Bigsort2.Contracts
{
    public interface IPooled<out T>
    {
        T Value { get; }
        void Free();
    }
}
