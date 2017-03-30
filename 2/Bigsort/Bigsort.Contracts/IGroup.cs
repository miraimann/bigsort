using System;

namespace Bigsort.Contracts
{
    public interface IGroup
        : IFixedSizeList<byte>
        , IDisposable
    {
        byte[][] Content { get; }
        int ContentRowsCount { get; }
        int ContentRowLength { get; }
        int LinesCount { get; }

        ulong ReadUInt64(int i);
        ulong ReadLittleEndianUInt64(int i);
        ulong ReadBigEndianUInt64(int i);

        uint ReadUInt32(int i);
        uint ReadLittleEndianUInt32(int i);
        uint ReadBigEndianUInt32(int i);

        ushort ReadUInt16(int i);
        ushort ReadLittleEndianUInt16(int i);
        ushort ReadBigEndianUInt16(int i);
    }
}
