namespace Bigsort.Contracts
{
    public interface IConfig
    {
        string GroupsFilePath { get; }
         
        string SortingSegment { get; }

        int BufferSize { get; }

        long MaxMemoryForLines { get; }

        int GroupBufferRowReadingEnsurance { get; }
    }
}
