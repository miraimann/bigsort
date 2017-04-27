namespace Bigsort.Contracts
{
    public interface IGroupsLoaderMaker
    {
        IGroupsLoader Make(string groupFilePath,
            IGroupsSummaryInfo groupsSummary,
            IGroup[] output);
    }
}
