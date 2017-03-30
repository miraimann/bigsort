namespace Bigsort.Contracts
{
    public interface IBitReader
    {
        ulong ReadEnviromentUInt64(int i);
        ulong ReadLittleEndianUInt64(int i);
        ulong ReadBigEndianUInt64(int i);


        uint ReadEnviromentUInt32(int i);
        uint ReadLittleEndianUInt32(int i);
        uint ReadBigEndianUInt32(int i);

        ushort ReadEnviromentUInt16(int i);
        ushort ReadLittleEndianUInt16(int i);
        ushort ReadBigEndianUInt16(int i);
    }
}
