namespace Bigsort.Contracts
{
    public interface IPooled<out T>
    {
        T Value { get; }
        void Free();
    }
}
