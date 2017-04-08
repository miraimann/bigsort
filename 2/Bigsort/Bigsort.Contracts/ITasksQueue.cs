using System;

namespace Bigsort.Contracts
{
    public interface ITasksQueue
    {
        int MaxThreadsCount { get; }

        bool IsProcessing { get; }

        void Enqueue(Action action);
    }
}
