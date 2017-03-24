namespace Bigsort.Contracts
{
    internal interface IBytesConverter<T>
    {
        T FromBytes(byte[] buff, int offset);
        byte[] ToBytes(T value);
    }
}
