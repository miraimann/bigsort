namespace Bigsort.Contracts
{
    public interface IPartsForSortIncrementor
    {
        void Increment(int linesOffset, int linesCount);
    }
}
