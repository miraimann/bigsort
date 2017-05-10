namespace Bigsort.Contracts
{
    internal interface IConfig
    {
        string InputFilePath { get; }
        string OutputFilePath { get; }
        string GroupsFilePath { get; }

        int PhysicalBufferLength { get; }
        int UsingBufferLength { get; }
        int GrouperEnginesCount { get; }
        int MaxRunningTasksCount { get; }
    }
}
