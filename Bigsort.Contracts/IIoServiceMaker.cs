namespace Bigsort.Contracts
{
    public interface IIoServiceMaker
    {
        IIoService Make(IPool<byte[]> buffersPool);
    }
}
