namespace Bigsort.Contracts
{
    public interface IBuffersPool
    {
        int Count { get; }

        IUsingHandle<byte[]> GetBuffer();
        IUsingHandle<byte[][]> TryGetBuffers(int count);

        void Free(int count);
    }
}
