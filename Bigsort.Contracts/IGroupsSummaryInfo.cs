namespace Bigsort.Contracts
{
    public interface IGroupsSummaryInfo
    {
        GroupInfo[] GroupsInfo { get; }
        int MaxGroupLinesCount { get; }
        int MaxGroupSize { get; }
    }
}
