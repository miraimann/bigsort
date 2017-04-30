namespace Bigsort.Contracts
{
    public interface IGroupsLoaderMaker
    {
        IGroupsLoader Make(
            string groupFilePath,
            IGroupsSummaryInfo groupsSummary,
            IGroup[] output,
            LineIndexes[] lines,
            ulong[] sortingSegments,
            IRangablePool<byte[]> buffersPool);
    }
}
