namespace Bigsort.Contracts
{
    internal interface ISortingSegmentsSupplier
    {
        void SupplyNext(IGroup group, int offset, int count);
    }
}
