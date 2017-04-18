namespace Bigsort.Contracts
{
    public struct LongRange
    {
        public LongRange(long offset, long count)
        {
            Offset = offset;
            Length = count;
        }

        public long Offset { get; }
        public long Length { get; }
    }
}
