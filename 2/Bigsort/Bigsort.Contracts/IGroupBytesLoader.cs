namespace Bigsort.Contracts
{
    public interface IGroupBytesLoader
    {
        IGroupBytesMatrix LoadMatrix(IGroupBytesMatrixInfo seed);
        IGroupBytesMatrixInfo CalculateMatrixInfo(IGroupInfo seed);
    }
}
