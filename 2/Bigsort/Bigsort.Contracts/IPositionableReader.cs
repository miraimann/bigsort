namespace Bigsort.Contracts
{
    public interface IPositionableReader
        : IReader
    {
        long Possition { get; set; }

        long Length { get; }
    }
}
