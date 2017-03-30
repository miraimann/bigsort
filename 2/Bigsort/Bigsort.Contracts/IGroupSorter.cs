namespace Bigsort.Contracts
{
    public interface IGroupSorter
    {
        void Sort(IGroup group,
                  ArrayFragment<SortingLine> linesFragment);
    }
}
