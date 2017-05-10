namespace Bigsort.Contracts
{
    public interface IPool<T>
    {
        int Count { get; }

        Handle<T> Get();

        Handle<T> TryGet();

        T TryExtract();

        T[] ExtractAll();
    }
}
