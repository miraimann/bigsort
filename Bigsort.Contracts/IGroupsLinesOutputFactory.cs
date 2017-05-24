namespace Bigsort.Contracts
{
    internal interface IGroupsLinesOutputFactory
    {
        IGroupsLinesOutput Create(long fileOffset = 0);
    }
}
