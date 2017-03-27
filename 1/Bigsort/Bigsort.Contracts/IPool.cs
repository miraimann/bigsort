namespace Bigsort.Contracts
{
    internal interface IPool<T>
    {
        IPooled<T> Get();
    }
}
