namespace Bigsort.Contracts
{
    public interface IGroupHashCoder
    {
        ulong GetStringHashCode(
            IBytesMatrix matrix, 
            int offset, 
            int count);
    }
}
