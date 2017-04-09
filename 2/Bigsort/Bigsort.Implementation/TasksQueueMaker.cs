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

            public Queue(int maxThreadsCount)
            {
                MaxThreadsCount = maxThreadsCount;
                _tasksQueue = new ConcurrentQueue<Action>();
                _runPermits = new ConcurrentBag<object>(
                    Enumerable.Range(0, MaxThreadsCount)
                              .Select(i => (object)i));
            }

            public int MaxThreadsCount { get; }

            public bool IsProcessing =>
                !_tasksQueue.IsEmpty ||
                _runPermits.Count != MaxThreadsCount;

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
            private readonly ConcurrentQueue<Action> _hightQueue, _lowQueue;

            public PriorityQueue(int maxThreadsCount)
            {
                MaxThreadsCount = maxThreadsCount;
                _hightQueue = new ConcurrentQueue<Action>();
                _lowQueue = new ConcurrentQueue<Action>();
                _runPermits = new ConcurrentBag<object>(
                    Enumerable.Range(0, MaxThreadsCount)
                              .Select(_ => new object()));
            }

            public int MaxThreadsCount { get; }

            public bool IsProcessing => 
                   !_lowQueue.IsEmpty
                || !_hightQueue.IsEmpty
                || _runPermits.Count != MaxThreadsCount;

            public void EnqueueHight(Action action)
            {
                _hightQueue.Enqueue(action);
                StartProcess();
            }

            public void EnqueueLow(Action action)
            {
                _lowQueue.Enqueue(action);
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
                while (_hightQueue.TryDequeue(out action) ||
                         _lowQueue.TryDequeue(out action))
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

                public int MaxThreadsCount =>
                    _implementation.MaxThreadsCount;

                public bool IsProcessing =>
                    _implementation.IsProcessing;

                public void Enqueue(Action action) =>
                    _implementation.EnqueueHight(action);
            }
        }
    }
}
