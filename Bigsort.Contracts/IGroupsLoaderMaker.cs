namespace Bigsort.Contracts
{
    internal interface IGroupsLoaderMaker
    {
        IGroupsLoader Make(GroupInfo[] groupsInfo, IGroup[] output);
    }
}
