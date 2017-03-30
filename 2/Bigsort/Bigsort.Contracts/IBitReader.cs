namespace Bigsort.Contracts
{
    public interface IBitReader
    {
        uint ReadLittleEndianUInt32(int i);
        uint ReadBigEndianUInt32(int i);
    }
}
