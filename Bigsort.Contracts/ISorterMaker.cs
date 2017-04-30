namespace Bigsort.Contracts
{
    public interface ISorterMaker
    {
        ISorter Make(IPool<byte[]> buffersPool);
    }
}
