namespace Bigsort.Contracts
{
    public interface IByteHashCoder
    {
        int HashCodesCount { get; }
        byte GetHashCodeFor(byte x);
    }
}
