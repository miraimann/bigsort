namespace Bigsort2.Contracts
{
    public interface IPool<T>
    {
        IPooled<T> Get();
    }
}
