using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class TasksQueueMaker
        : ITasksQueueMaker
    {
        public ITasksQueue Make(int maxRunningTasksCount) =>
            new Queue(maxRunningTasksCount);

        public class Queue
            : ITasksQueue
        {
            private readonly ConcurrentBag<object> _runPermits;
            private readonly ConcurrentQueue<Action> _tasksQueue;
            private readonly int _maxRunningTasksCount;

            public Queue(int maxRunningTasksCount)
            {
                _maxRunningTasksCount = maxRunningTasksCount;
                _tasksQueue = new ConcurrentQueue<Action>();
                _runPermits = new ConcurrentBag<object>(
                    Enumerable.Range(0, maxRunningTasksCount)
                        .Select(_ => new object()));
            }

            public bool IsProcessing =>
                !_tasksQueue.IsEmpty ||
                _runPermits.Count != _maxRunningTasksCount;

            public async void Enqueue(Action action)
            {
                _tasksQueue.Enqueue(action);

                object runPermit;
                if (_runPermits.TryTake(out runPermit))
                    _runPermits.Add(await TryStartNewTask(runPermit));
            }

            private async Task<object> TryStartNewTask(object runPermit)
            {
                Action action;
                while (_tasksQueue.TryDequeue(out action))
                    await Task.Run(action);
                return runPermit;
            }
        }
    }
}
