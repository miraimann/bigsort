namespace Bigsort.Contracts
{
    internal interface IPool<T>
    {
        int Count { get; }

        Handle<T> Get();

        Handle<T> TryGet();

        T TryExtract();

        T[] ExtractAll();
    }
}
