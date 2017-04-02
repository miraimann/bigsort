namespace Bigsort.Contracts
{
    public interface ISegmentServiceFactory
    {
        ISegmentService<byte> CreateForByte();
        ISegmentService<uint> CreateForInt32();
        ISegmentService<ulong> CreateForInt64();
    }
}
