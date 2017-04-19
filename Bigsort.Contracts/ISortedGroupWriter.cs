namespace Bigsort.Contracts
{
    public interface ISortedGroupWriter
    {
        void Write(IGroupMatrix group, 
                   Range linesRange, 
                   IFileWriter output);
    }
}
