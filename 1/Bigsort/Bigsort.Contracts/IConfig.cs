namespace Bigsort.Contracts
{
    internal interface IConfig
    {
        int MaxCollectionSize { get; }

        int IntsAccumulatorFragmentSize { get; }

        int BytesEnumeratingBufferSize { get; }

        int ResultWriterBufferSize { get; }

        byte[] EndLine { get; }

        byte Dot { get; }
    }
}
