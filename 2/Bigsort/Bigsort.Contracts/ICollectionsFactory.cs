namespace Bigsort.Contracts
{
    public interface ICollectionsFactory
    {
        IFragmentedList<T> CreateFragmentedList<T>() 
            where T : struct ;
    }
}
