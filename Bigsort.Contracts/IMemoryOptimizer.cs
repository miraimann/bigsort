namespace Bigsort.Contracts
{
    public interface IMemoryOptimizer
    {
        void OptimizeMemoryForSort(
            int maxGroupSize, 
            int maxGroupLinesCount);
    }
}
