namespace Bigsort.Contracts
{
    public interface IGroupBytesLoader
    {
        IGroupBytes Load(IGroupInfo seed);
    }
}
