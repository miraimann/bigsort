namespace Bigsort.Contracts
{
    public interface ISortingFragmentsMoverMaker
    {
        ISortingFragmentsMover Make(
            IFixedSizeList<byte> group,
            SortingLine[] lines);
    }
}
