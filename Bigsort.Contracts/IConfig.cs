namespace Bigsort.Contracts
{
    public interface IConfig
    {
        string GroupsFileDirectoryPath { get; }

        int PhysicalBufferLength { get; }

        int UsingBufferLength { get; }

        int GrouperEnginesCount { get; }

        int MaxRunningTasksCount { get; }

        int BufferReadingEnsurance { get; }
    }
}
