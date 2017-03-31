namespace Bigsort.Contracts
{
    public interface IFragmentsPool<T>
    {
        IPooled<ArrayFragment<T>> TryGet(int length);
    }
}
