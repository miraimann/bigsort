namespace Bigsort.Contracts
{
    public interface IGroupBytesMatrixService
    {
        IGroupBytesMatrixRowsInfo CalculateRowsInfo(int bytesCount);

        IGroupBytesMatrix LoadMatrix(
            IGroupBytesMatrixRowsInfo rowsInfo,
            IGroupInfo groupInfo,
            IReader groupsFileReader);
    }
}
