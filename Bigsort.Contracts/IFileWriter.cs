using System;

namespace Bigsort.Contracts
{
    internal interface IFileWriter
        : IDisposable
    {
        long Position { get; set; }
        long Length { get; }

        void Write(byte[] array, int offset, int count);
        void Flush();
    }
}
