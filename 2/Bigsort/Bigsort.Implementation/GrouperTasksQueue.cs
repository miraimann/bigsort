using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GrouperTasksQueue
        : IGrouperTasksQueue
    {
        // private readonly IPriorityTasksQueue _implementation;
        private readonly ITasksQueue _implementation;

        public GrouperTasksQueue(
            ITasksQueueMaker tasksQueueMaker)
        {
            _implementation = tasksQueueMaker
                   .MakeQueue(Environment.ProcessorCount);
               // .MakePriorityQueue(1);
               // .MakePriorityQueue(Environment.ProcessorCount);
        }

        public int MaxThreadsCount =>
            _implementation.MaxThreadsCount;

        public bool IsProcessing =>
            _implementation.IsProcessing;

        public void Enqueue(Action action) =>
            _implementation.Enqueue(action);

        // public void EnqueueHight(Action action) =>
        //     _implementation.EnqueueHight(action);

        // public void EnqueueLow(Action action) =>
        //    _implementation.EnqueueLow(action);

        // public ITasksQueue AsLowQueue() =>
        //     _implementation.AsLowQueue();

        // public ITasksQueue AsHightQueue() =>
        //     _implementation.AsHightQueue();
    }
}
