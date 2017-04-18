namespace Bigsort.Contracts
{
    public interface IGrouperBuffersProviderMaker
    {
        IGrouperBuffersProvider Make(string path, int buffLength);
        IGrouperBuffersProvider Make(string path, int buffLength, 
            long fileOffset, long readingLength);
    }
}
