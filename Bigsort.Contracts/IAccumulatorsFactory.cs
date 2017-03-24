namespace Bigsort.Contracts
{
    internal interface IAccumulatorsFactory
    {
        IAccumulator<int> CreateForInt();
        ICacheableAccumulator<int> CreateCacheableForInt();
        ICacheableAccumulator<long> CreateCacheableForLong();
    }
}
