namespace Bigsort.Contracts
{
    public interface ISortingSegmentsSupplier
    {
        void SupplyNext(IGroupBytes group, Range linesRange);
    }
}
