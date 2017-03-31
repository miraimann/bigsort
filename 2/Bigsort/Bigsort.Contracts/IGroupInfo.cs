namespace Bigsort.Contracts
{
    public interface IGroupInfo
    {
        string Name { get; }
        int ContentRowsCount { get; }
        int ContentRowLength { get; }
        int LinesCount { get; }
        int BytesCount { get; }
    }
}
