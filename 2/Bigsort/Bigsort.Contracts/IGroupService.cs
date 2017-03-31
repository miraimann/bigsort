namespace Bigsort.Contracts
{
    public interface IGroupService
    {
        IGroup LoadGroup(string path);
        int LinesCountOfGroup(string path);
    }
}
