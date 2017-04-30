namespace Bigsort.Contracts
{
    public interface IGrouperMaker
    {
        IGrouper Make(IPool<byte[]> buffersPool);
    }
}
