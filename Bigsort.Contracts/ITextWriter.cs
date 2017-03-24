using System;

namespace Bigsort.Contracts
{
    internal interface ITextWriter
        : IDisposable
    {
        void WriteLine(string format, params object[] args);
        void WriteLine();
    }
}
