using System;

namespace Bigsort.Contracts
{
    public interface IPriorityTasksQueue
        : ITasksQueue
    {
        void EnqueueHight(Action action);
        void EnqueueLow(Action action);

        ITasksQueue AsLowQueue();

        ITasksQueue AsHightQueue();
    }
}
