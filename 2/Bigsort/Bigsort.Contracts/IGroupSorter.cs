namespace Bigsort.Contracts
{
    public interface IGroupSorter
    {
        void Sort(IGroupBytes group, Range linesRange);
    }
}
