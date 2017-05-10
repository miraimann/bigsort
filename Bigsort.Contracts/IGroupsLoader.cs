using System;

namespace Bigsort.Contracts
{
    internal interface IGroupsLoader
        : IDisposable
    {
        Range LoadNextGroups();
    }
}
