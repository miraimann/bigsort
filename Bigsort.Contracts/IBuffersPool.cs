namespace Bigsort.Contracts
{
    public interface IBuffersPool
    {
        IUsingHandle<byte[]> GetBuffer();
    }
}
