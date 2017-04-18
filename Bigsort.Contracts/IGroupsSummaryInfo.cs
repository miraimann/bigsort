namespace Bigsort.Contracts
{
    public interface IGroupsSummaryInfo
    {
        IGroupInfo[] GroupsInfo { get; }
        int MaxGroupLinesCount { get; }
        int MaxGroupSize { get; }
    }
}
