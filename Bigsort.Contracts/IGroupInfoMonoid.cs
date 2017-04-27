namespace Bigsort.Contracts
{
    public interface IGroupInfoMonoid
    {
        GroupInfo Null { get; }
        GroupInfo Append(GroupInfo a, GroupInfo b);
    }
}
