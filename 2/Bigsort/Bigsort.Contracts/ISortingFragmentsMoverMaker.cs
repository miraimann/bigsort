namespace Bigsort.Contracts
{
    public interface ISortingFragmentsMoverMaker
    {
        ISortingFragmentsMover Make(
            IBytesMatrix group,
            SortingLine[] lines);
    }
}
