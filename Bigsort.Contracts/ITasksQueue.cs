using System;

namespace Bigsort.Contracts
{
    internal interface ITasksQueue
    {
        void Enqueue(Action action);
    }
}
