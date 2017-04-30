namespace Bigsort.Contracts
{
    public interface IBuffersPool
        : IPool<byte[]>
    {
        byte[] Create();
    }
}
