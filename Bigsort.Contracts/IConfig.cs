namespace Bigsort.Contracts
{
    public interface IConfig
    {
        string SortingSegment { get; }

        int BufferSize { get; }

        long MaxMemoryForLines { get; }

        int GrouperEnginesCount { get; }

        int MaxRunningTasksCount { get; }

        int GroupBufferRowReadingEnsurance { get; }
    }
}
