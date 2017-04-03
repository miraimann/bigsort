namespace Bigsort.Contracts
{
    public interface IGroupInfo
    {
        string Name { get; }
        int LinesCount { get; }
        int BytesCount { get; }
    }
}
