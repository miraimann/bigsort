namespace Bigsort.Contracts
{
    public interface IMultiUsingHandle<out T>
        : IUsingHandle<T>
    {
        IUsingHandle<T> SubUse();
    }
}
