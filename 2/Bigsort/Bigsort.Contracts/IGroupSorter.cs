namespace Bigsort.Contracts
{
    public interface IGroupSorter
    {
        void Sort(IFixedSizeList<byte> group,
                  ArrayFragment<SortingLine> linesFragment);
    }
}
