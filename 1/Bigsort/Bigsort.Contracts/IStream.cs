namespace Bigsort.Contracts
{
    internal interface IStream
        : IReadingStream
        , IWritingStream
    {
        new long Position { get; set; }
    }
}
