namespace Bigsort.Contracts
{
    public interface ISortedGroupWriterMaker
    {
        ISortedGroupWriter Make(string outputFilepath);
    }
}
