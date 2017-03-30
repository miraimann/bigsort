namespace Bigsort.Contracts
{
    public interface IPartsForSortIncrementorMaker
    {
        IPartsForSortIncrementor Make(
            SortingLineView[] lines,
            IBytesMatrix group);
    }
}
