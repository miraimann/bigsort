namespace Bigsort.Contracts
{
    public interface ILinesIndexesExtractor
    {
        void ExtractIndexes(
            IFixedSizeList<byte> group,
            Range linesRange);
    }
}
