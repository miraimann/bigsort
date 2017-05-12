namespace Bigsort.Contracts
{
    internal interface IBuffersPool
    {
        int Count { get; }

        Handle<byte[]> Get();

        Handle<byte[]> TryGet();

        byte[][] ExtractAll();
    }
}
