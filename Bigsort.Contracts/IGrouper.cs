namespace Bigsort.Contracts
{
    public interface IGrouper
    {
        IGroupsSummaryInfo SplitToGroups(
            string inputFile, string groupsFile);
    }
}
