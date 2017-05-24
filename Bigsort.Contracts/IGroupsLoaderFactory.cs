namespace Bigsort.Contracts
{
    internal interface IGroupsLoaderFactory
    {
        IGroupsLoader Create(GroupInfo[] groupsInfo, IGroup[] output);
    }
}
