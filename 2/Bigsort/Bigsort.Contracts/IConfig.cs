namespace Bigsort.Contracts
{
    public interface IConfig
    {
        string PartsDirectory { get; }
         
        int BufferSize { get; }

        int MainArraySize { get; }

        int MaxLoadedGroupsSize { get; }

        int MaxGroupsBuffersCount { get; }

        int MaxTasksCount { get; }

        int GroupBufferRowReadingEnsurance { get; }

        bool IsLittleEndian { get; }
    }
}
