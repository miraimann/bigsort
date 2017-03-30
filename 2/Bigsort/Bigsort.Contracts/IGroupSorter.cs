namespace Bigsort.Contracts
{
    public interface IGroupSorter
    {
        void Sort(IBytesMatrix group,
                  ArrayFragment<SortingLine> linesFragment);
    }
}
