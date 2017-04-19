namespace Bigsort.Contracts
{
    public interface ISorter
    {
        void Sort(
            string groupsFilePath,
            IGroupsSummaryInfo groupsSummary,
            string outputPath);
    }
}
