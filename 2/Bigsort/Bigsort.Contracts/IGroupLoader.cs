namespace Bigsort.Contracts
{
    public interface IGroupLoader
    {
        IGroup Load(IGroupInfo seed);
    }
}
