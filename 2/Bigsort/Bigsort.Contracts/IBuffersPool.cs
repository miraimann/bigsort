namespace Bigsort.Contracts
{
    public interface IBuffersPool
    {
        IDisposableValue<byte[]> GetBuffer();
    }
}
