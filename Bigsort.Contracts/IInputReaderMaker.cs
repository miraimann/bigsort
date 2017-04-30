namespace Bigsort.Contracts
{
    public interface IInputReaderMaker
    {
        IInputReader Make(long groupsFileLength);
        IInputReader Make(long groupsFileOffset, long readingLength);
    }
}
