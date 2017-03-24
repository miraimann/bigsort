namespace Bigsort.Contracts
{
    internal interface IBytesConvertersFactory
    {
        IBytesConverter<int> CreateForInt();
        IBytesConverter<long> CreateForLong();
    }
}
