namespace Bigsort.Contracts
{
    public interface IBitReader
    {
        ushort ReadUInt16(int i);
        uint ReadUInt32(int i);
        ulong ReadUInt64(int i);
    }
}
