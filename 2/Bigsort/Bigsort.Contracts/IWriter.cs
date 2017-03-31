using System;

namespace Bigsort.Contracts
{
    public interface IWriter
        : IDisposable
    {
        long Length { get; }

        void Write(byte[] array, int offset, int count);
        void Write(byte x);
        void Flush();
    }
}
