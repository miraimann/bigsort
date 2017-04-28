namespace Bigsort.Contracts
{
    public interface IRangesPool
    {
        int Length { get; }

        IUsingHandle<Range> TryGet(int length);
    }
}
