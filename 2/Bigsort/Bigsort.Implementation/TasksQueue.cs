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
        private readonly IConfig _config;
        
        public TasksQueue(IConfig config)
        {
            _config = config;
            _tasksQueue = new ConcurrentQueue<Action>();
            _runPermits = new ConcurrentBag<object>(
                Enumerable.Range(0, _config.MaxTasksCount)
                          .Select(_ => new object()));
        }

        public bool IsProcessing =>
            !_tasksQueue.IsEmpty ||
            _runPermits.Count != _config.MaxTasksCount;

        public void Enqueue(Action action)
        {
            _tasksQueue.Enqueue(action);
            TryStartNewTask();
        }

        private void TryStartNewTask()
        {
            object runPermit;
            if (!_runPermits.TryTake(out runPermit))
                return;

            Action action;
            if (_tasksQueue.TryDequeue(out action))
                Task.Run(() =>
                {
                    action();
                    _runPermits.Add(runPermit);
                    TryStartNewTask();
                });
            else _runPermits.Add(runPermit);
        }
    }
}
