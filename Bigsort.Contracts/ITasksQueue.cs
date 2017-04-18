using System;

namespace Bigsort.Contracts
{
    public interface ITasksQueue
    {
        void Enqueue(Action action);
    }
}
