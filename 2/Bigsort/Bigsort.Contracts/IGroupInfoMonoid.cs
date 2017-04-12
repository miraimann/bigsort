namespace Bigsort.Contracts
{
    public interface IGroupInfoMonoid
    {
        IGroupInfo Null { get; }
        IGroupInfo Append(IGroupInfo a, IGroupInfo b);
    }
}
