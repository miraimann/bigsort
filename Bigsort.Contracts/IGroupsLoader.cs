using System;

namespace Bigsort.Contracts
{
    public interface IGroupsLoader
        : IDisposable
    {
        Range LoadNextGroups();
    }
}
