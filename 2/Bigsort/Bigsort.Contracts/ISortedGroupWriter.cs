namespace Bigsort.Contracts
{
    public interface ISortedGroupWriter
    {
        void Write(IGroupBytes group, 
                   Range linesRange, 
                   IWriter output);
    }
}
