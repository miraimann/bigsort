using System;

namespace Bigsort.Contracts
{
    internal interface IReadingStream
        : IDisposable
    {
        long Length { get; }
        long Position { get; set; }
        int Read(byte[] array, int offset, int count);
        int ReadByte();
    }
}
