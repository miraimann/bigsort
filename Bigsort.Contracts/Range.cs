namespace Bigsort.Contracts
{
    public struct Range
    {
        public Range(int offset, int count)
        {
            Offset = offset;
            Length = count;
        }

        public int Offset { get; }
        public int Length { get; }
    }
}
