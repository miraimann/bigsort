namespace Bigsort.Contracts
{
    public interface IGroupSorter
    {
        void Sort(IGroupMatrix group, Range linesRange);
    }
}
