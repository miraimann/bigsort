namespace Bigsort.Contracts
{
    internal interface IInputReaderMaker
    {
        IInputReader Make(long groupsFileLength);
        IInputReader Make(long groupsFileOffset, long readingLength);
    }
}
