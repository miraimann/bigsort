﻿namespace Bigsort.Contracts
{
    public interface IConfig
    {
        string PartsDirectory { get; }
         
        int BufferSize { get; }

        int MainArraySize { get; }

        int MaxLoadedGroupsSize { get; }

        int MaxTasksCount { get; }
    }
}
