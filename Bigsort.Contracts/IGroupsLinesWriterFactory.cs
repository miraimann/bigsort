namespace Bigsort.Contracts
{
    public interface IGroupsLinesWriterFactory
    {
        IGroupsLinesWriter Create(long fileOffset = 0);
    }
}
