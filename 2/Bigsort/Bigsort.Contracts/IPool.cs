namespace Bigsort.Contracts
{
    public interface IPool<T>
    {
        IPooled<T> Get();
    }
}
