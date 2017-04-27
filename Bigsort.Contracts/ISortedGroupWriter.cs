using System;

namespace Bigsort.Contracts
{
    public interface ISortedGroupWriter
        : IDisposable
    {
        void Write(IGroup group, long position);
    }
}
