namespace Bigsort.Contracts
{
    public interface ISortedGroupWriter
    {
        void Write(
            IGroup group,
            ArrayFragment<SortingLine> lines,
            IWriter output);
    }
}
