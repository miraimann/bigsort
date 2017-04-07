using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Bigsort.Tools.TestFileGenerator
{
    class Program
    {
        static int Main(string[] args)
        {
            var queue = new PriorityQueue(2);
            var low = queue.AsLowQueue();
            var hight = queue.AsHightQueue();

            low.Enqueue(() => {});
            hight.Enqueue(() => { });

            if (args.Length != 3)
            {
                Console.WriteLine("invalid args");
                return 1;
            }

            Generator.Generate(args[0], args[1], args[2]);

            return 0;
        }

        public interface ITasksQueue
        {
            bool IsProcessing { get; }

            void Enqueue(Action action);
        }

        public interface IPriorityTasksQueue
            : ITasksQueue
        {
            void EnqueueHight(Action action);
            void EnqueueLow(Action action);

            ITasksQueue AsLowQueue();

            ITasksQueue AsHightQueue();
        }

        private class PriorityQueue
            : IPriorityTasksQueue
                , PriorityQueue.IHightQueue
                , PriorityQueue.ILowQueue
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

            void ITasksQueue.Enqueue(Action action) =>
                EnqueueLow(action);

            void ILowQueue.Enqueue(Action action) =>
                EnqueueLow(action);

            void IHightQueue.Enqueue(Action action) =>
                EnqueueHight(action);

            public ITasksQueue AsLowQueue() =>
                (ILowQueue) this;

            public ITasksQueue AsHightQueue() =>
                (IHightQueue) this;

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

            public interface ILowQueue
                : ITasksQueue
            {
                new void Enqueue(Action action);
            }

            public interface IHightQueue
                : ITasksQueue
            {
                new void Enqueue(Action action);
            }
        }

    }
}
