using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class TasksQueue
        : ITasksQueue
    {
        private readonly ConcurrentBag<object> _runPermits;
        private readonly ConcurrentQueue<Action> _tasksQueue;
        private readonly int _maxRunningTasksCount;

        public TasksQueue(IConfig config)
        {
            _maxRunningTasksCount = config.MaxRunningTasksCount;
            _tasksQueue = new ConcurrentQueue<Action>();
            _runPermits = new ConcurrentBag<object>(
                Enumerable.Range(0, _maxRunningTasksCount)
                    .Select(i => (object) i));
        }
        
        public bool IsProcessing =>
            !_tasksQueue.IsEmpty ||
            _runPermits.Count != _maxRunningTasksCount;

        public async void Enqueue(Action action)
        {
            _tasksQueue.Enqueue(action);

            object runPermit;
            if (_runPermits.TryTake(out runPermit))
                _runPermits.Add(await Process(runPermit));
        }

        private async Task<object> Process(object runPermit)
        {
            Action action;
            while (_tasksQueue.TryDequeue(out action))
                await Task.Run(action);
            return runPermit;
        }
    }
}
