namespace Bigsort.Contracts
{
    public interface IGroupMatrixService
    {
        int RowsCountFor(int bytesCount);

        bool TryCreateMatrix(IGroupInfo groupInfo, out IGroupMatrix matrix);

        void LoadGroupToMatrix(IGroupMatrix matrix, IGroupInfo groupInfo,
            IFileReader groupsFileReader);
    }
}
