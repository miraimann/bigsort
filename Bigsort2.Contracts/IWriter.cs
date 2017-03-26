using System;

namespace Bigsort2.Contracts
{
    public interface IWriter
        : IDisposable
    {
        void Write(byte[] array, int offset, int count);
        void Write(byte x);
    }
}
