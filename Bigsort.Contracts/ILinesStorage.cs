namespace Bigsort.Contracts
{
    public interface ILinesStorage
        : ILinesIndexesStorage
    {
        int LineSize { get; }
    }

    public interface ILinesStorage<out TSegment>
        : ILinesStorage
    {
        TSegment[] Segments { get; }
    }
}
