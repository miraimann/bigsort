using System;

namespace Bigsort.Contracts
{
    internal interface ISortedGroupWriter
        : IDisposable
    {
        void Write(IGroup group, long position);
    }
}
