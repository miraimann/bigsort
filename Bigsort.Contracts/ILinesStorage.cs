namespace Bigsort.Contracts
{
    public interface ILinesStorage<out TSegment>
        : ILinesIndexesStorage
    {
        TSegment[] Segments { get; }
    }
}
