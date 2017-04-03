namespace Bigsort.Contracts
{
    public interface ISortingSegmentsSupplier
    {
        void SupplyNext(IGroupBytesMatrix group, Range linesRange);
        void SupplyNext(IGroupBytesMatrix group, int offset, int count);
    }
}
