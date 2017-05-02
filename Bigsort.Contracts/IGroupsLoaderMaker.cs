namespace Bigsort.Contracts
{
    public interface IGroupsLoaderMaker
    {
        IGroupsLoader Make(GroupInfo[] groupsInfo, IGroup[] output);
    }
}
