namespace Bigsort.Contracts
{
    public interface IGroupsLinesWriterMaker
    {
        IGroupsLinesWriter Make(string path, long fileOffset = 0);
    }
}
