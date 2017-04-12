namespace Bigsort.Contracts
{
    public interface IPositionableReader
        : IReader
    {
        long Length { get; }
    }
}
