using System;

namespace Bigsort.Contracts
{
    public interface ITasksQueue
    {
        bool IsProcessing { get; }
        void Enqueue(Action action);
    }
}
