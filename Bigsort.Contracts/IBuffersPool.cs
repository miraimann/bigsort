namespace Bigsort.Contracts
{
    public interface IBuffersPool
    {
        IUsingHandle<byte[]> GetBuffer();
        IUsingHandle<byte[][]> TryGetBuffers(int count);
    }
}
