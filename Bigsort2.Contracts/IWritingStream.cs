using System;

namespace Bigsort.Contracts
{
    internal interface IWritingStream
        : IDisposable
    {
        long Position { get; set; }

        void Write(byte[] array, int offset, int count);
        void WriteByte(byte x);
    }
}
