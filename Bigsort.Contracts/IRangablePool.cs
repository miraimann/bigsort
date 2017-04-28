namespace Bigsort.Contracts
{
    public interface IRangablePool<out T>
    {
        int Count { get; }

        IUsingHandle<T[]> TryGet(int count);

        IUsingHandle<T[]> Get(int count);

        T[] TryExtract(int count);
    }
}
