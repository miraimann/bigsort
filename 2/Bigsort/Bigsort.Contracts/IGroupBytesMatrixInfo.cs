namespace Bigsort.Contracts
{
    public interface IGroupBytesMatrixInfo
        : IGroupInfo
    {
        int RowsCount { get; }
        int RowLength { get; }
    }
}
