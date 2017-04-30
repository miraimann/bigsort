namespace Bigsort.Contracts
{
    public interface IGroupsLoaderMaker
    {
        IGroupsLoader Make(IGroupsSummaryInfo groupsSummary, IGroup[] output);
    }
}
