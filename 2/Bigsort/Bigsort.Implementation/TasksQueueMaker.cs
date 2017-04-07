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
        public ITasksQueue MakeQueue(int maxThreadsCount) =>
            new Queue(maxThreadsCount);

        public IPriorityTasksQueue MakePriorityQueue(int maxThreadsCount) =>
            new PriorityQueue(maxThreadsCount);

        private class Queue
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

        private class PriorityQueue
            : IPriorityTasksQueue
        {
            private readonly ConcurrentBag<object> _runPermits;
            private readonly ConcurrentQueue<Action> _hightTasksQueue, _lowTasksQueue;
            private readonly int _maxRunningTasksCount;

            public PriorityQueue(int maxRunningTasksCount)
            {
                _maxRunningTasksCount = maxRunningTasksCount;
                _hightTasksQueue = new ConcurrentQueue<Action>();
                _lowTasksQueue = new ConcurrentQueue<Action>();
                _runPermits = new ConcurrentBag<object>(
                    Enumerable.Range(0, maxRunningTasksCount)
                              .Select(_ => new object()));
            }

            public bool IsProcessing => 
                   !_lowTasksQueue.IsEmpty
                || !_hightTasksQueue.IsEmpty
                || _runPermits.Count != _maxRunningTasksCount;

            public void EnqueueHight(Action action)
            {
                _hightTasksQueue.Enqueue(action);
                StartProcess();
            }

            public void EnqueueLow(Action action)
            {
                _lowTasksQueue.Enqueue(action);
                StartProcess();
            }

            public void Enqueue(Action action) =>
                EnqueueLow(action);
            
            public ITasksQueue AsHightQueue() =>
                new HightQueueAdapter(this);

            public ITasksQueue AsLowQueue() =>
                this;

            private async void StartProcess()
            {
                object runPermit;
                if (_runPermits.TryTake(out runPermit))
                    _runPermits.Add(await Process(runPermit));
            }

            private async Task<object> Process(object runPermit)
            {
                Action action;
                while (_hightTasksQueue.TryDequeue(out action) ||
                         _lowTasksQueue.TryDequeue(out action))
                    await Task.Run(action);
                return runPermit;
            }

            private class HightQueueAdapter
                : ITasksQueue
            {
                private readonly IPriorityTasksQueue _implementation;

                public HightQueueAdapter(IPriorityTasksQueue implementation)
                {
                    _implementation = implementation;
                }

                public bool IsProcessing =>
                    _implementation.IsProcessing;

                public void Enqueue(Action action) =>
                    _implementation.EnqueueHight(action);
            }
        }
    }
}
