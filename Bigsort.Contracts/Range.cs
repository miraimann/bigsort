namespace Bigsort.Contracts
{
    public struct Range
    {
        public Range(int offset, int count)
        {
            Offset = offset;
            Length = count;
        }

        public int Offset;
        public int Length;

        public static bool IsZero(Range x) =>
            x.Length == 0;
    }
}
