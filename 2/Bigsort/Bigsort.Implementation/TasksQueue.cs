using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class TasksQueue
        : ITasksQueue
    {
        private readonly ConcurrentQueue<Action> _tasksQueue;
        private readonly long _maxRunningTasksCount;
        private long _runningTasksCount = 0;

        public TasksQueue(IConfig config)
        {
            _maxRunningTasksCount = config.MaxRunningTasksCount;
            _tasksQueue = new ConcurrentQueue<Action>();
        }

        public void Enqueue(Action action)
        {
            if (Interlocked.Read(ref _runningTasksCount) < _maxRunningTasksCount)
            {
                Interlocked.Increment(ref _runningTasksCount);
                Task.Run(() =>
                {
                    action();
                    while (_tasksQueue.TryDequeue(out action)) action();
                    Interlocked.Decrement(ref _runningTasksCount);
                });
            }
            else _tasksQueue.Enqueue(action);
        }
    }
}
