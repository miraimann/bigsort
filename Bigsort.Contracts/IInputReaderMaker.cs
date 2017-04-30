namespace Bigsort.Contracts
{
    public interface IInputReaderMaker
    {
        IInputReader Make(
            string inputPath, 
            long fileLength,
            IPool<byte[]> buffersPool);

        IInputReader Make(
            string inputPath, 
            long fileOffset, 
            long readingLength, 
            IPool<byte[]> buffersPool);
    }
}
