namespace Bigsort.Contracts
{
    public interface IGroupInfo
    {
        string Name { get; }
        int RowsCount { get; }
        int RowLength { get; }
        int LinesCount { get; }
        int BytesCount { get; }
    }
}
