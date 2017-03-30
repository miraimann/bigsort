namespace Bigsort.Contracts
{
    public interface ISortingFragmentsMoverMaker
    {
        ISortingFragmentsMover Make(
            IGroup group,
            SortingLine[] lines);
    }
}
