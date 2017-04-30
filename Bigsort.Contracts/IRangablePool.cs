namespace Bigsort.Contracts
{
    public interface IRangablePool<out T>
        : IPool<T>
    {
        IUsingHandle<T[]> TryGet(int count);

        IUsingHandle<T[]> Get(int count);

        T[] TryExtract(int count);
    }
}
