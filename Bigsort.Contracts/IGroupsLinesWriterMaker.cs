namespace Bigsort.Contracts
{
    public interface IGroupsLinesWriterMaker
    {
        IGroupsLinesWriter Make(
            string groupsFilePath, 
            IPool<byte[]> buffersPool, 
            long fileOffset = 0);
    }
}
