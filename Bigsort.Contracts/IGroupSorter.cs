namespace Bigsort.Contracts
{
    public interface IGroupSorter
    {
        void Sort(IGroupBytesMatrix group, Range linesRange);
    }
}
