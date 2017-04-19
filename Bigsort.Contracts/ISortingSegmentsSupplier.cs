namespace Bigsort.Contracts
{
    public interface ISortingSegmentsSupplier
    {
        void SupplyNext(IGroupMatrix group, Range linesRange);
        void SupplyNext(IGroupMatrix group, int offset, int count);
    }
}
