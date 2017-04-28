namespace Bigsort.Contracts
{
    public interface IPool<out T>
    {
        int Count { get; }

        IUsingHandle<T> Get();

        IUsingHandle<T> TryGet();

        T TryExtract();

        T[] ExtractAll();
    }
}
