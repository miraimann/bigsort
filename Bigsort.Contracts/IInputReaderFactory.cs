namespace Bigsort.Contracts
{
    internal interface IInputReaderFactory
    {
        IInputReader Create(long groupsFileLength);
        IInputReader Create(long groupsFileOffset, long readingLength);
    }
}
