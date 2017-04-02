namespace Bigsort.Contracts
{
    public interface IConfig
    {
        string PartsDirectory { get; }
         
        int BufferSize { get; }

        long MaxMemoryForLines { get; }

        int MaxLoadedGroupsSize { get; }

        int MaxGroupsBuffersCount { get; }

        int MaxTasksCount { get; }

        int GroupBufferRowReadingEnsurance { get; }

        bool IsLittleEndian { get; }
    }
}
