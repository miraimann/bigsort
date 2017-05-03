namespace Bigsort.Contracts
{
    public interface ISortingSegmentsSupplier
    {
        void SupplyNext(IGroup group, int offset, int count);
    }
}
