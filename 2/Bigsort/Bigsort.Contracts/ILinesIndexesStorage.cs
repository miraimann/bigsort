namespace Bigsort.Contracts
{
    public interface ILinesIndexesStorage
    {
        int Length { get; }

        LineIndexes[] Indexes { get; }
    }
}
