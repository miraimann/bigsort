using System;

namespace Bigsort.Contracts
{
    public interface IGroup
        : IGroupInfo
        , IFixedSizeList<byte>
    {
        byte[][] Content { get; }

        void DisposeRow(int i);

        ulong Read8Bytes(int i);

        uint Read4Bytes(int i);

    }
}
