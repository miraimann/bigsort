namespace Bigsort.Contracts
{
    internal interface IGroupsLinesWriterFactory
    {
        IGroupsLinesWriter Create(long fileOffset = 0);
    }
}
