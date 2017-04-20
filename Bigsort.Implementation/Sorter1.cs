using System;
using System.Collections.Concurrent;
using System.Threading;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class Sorter1
        : ISorter
    {
        private readonly ILinesReservation _linesReservation;
        private readonly IPoolMaker _poolMaker;
        private readonly IMemoryOptimizer _memoryOptimizer;
        private readonly IGroupMatrixService _groupMatrixService;
        private readonly IGroupSorter _groupSorter;
        private readonly ISortedGroupWriter _sortedGroupWriter;
        private readonly IIoService _ioService;
        private readonly ITasksQueue _tasksQueue;
        private readonly IConfig _config;

        public Sorter1(
            ILinesReservation linesReservation,
            IGroupMatrixService groupMatrixService,
            IGroupSorter groupSorter,
            IMemoryOptimizer memoryOptimizer,
            ISortedGroupWriter sortedGroupWriter,
            IIoService ioService,
            ITasksQueue tasksQueue,
            IPoolMaker poolMaker, 
            IConfig config)
        {
            _linesReservation = linesReservation;
            _groupMatrixService = groupMatrixService;
            _groupSorter = groupSorter;
            _memoryOptimizer = memoryOptimizer;
            _sortedGroupWriter = sortedGroupWriter;
            _ioService = ioService;
            _tasksQueue = tasksQueue;
            _poolMaker = poolMaker;
            _config = config;
        }

        public void Sort(
            string groupsFilePath,
            IGroupsSummaryInfo groupsSummary,
            string outputPath)
        {
            _memoryOptimizer.OptimizeMemoryForSort(
                groupsSummary.MaxGroupSize,
                groupsSummary.MaxGroupLinesCount);

            var source = new ConcurrentBag<Group>();
            var loaded = new ConcurrentBag<LoadedGroup>();
            var sorted = new ConcurrentBag<LoadedGroup>();

            var groupsInfo = groupsSummary.GroupsInfo;
            
            long position = 0;
            for (int i = 0; i < Consts.MaxGroupsCount; i++)
            {
                var info = groupsInfo[i];
                if (info != null)
                {
                    source.Add(new Group(info, position));
                    position += info.BytesCount;
                }
            }
            
            using (var groupsReadersPool = _poolMaker.Make(
                                productFactory: () => _ioService.OpenRead(groupsFilePath),
                                productDestructor: reader => reader.Dispose()))

            using (var resultWritersPool = _poolMaker.Make(
                                productFactory: () => _ioService.OpenWrite(outputPath, buffering: true),
                                productDestructor: writer => writer.Dispose()))
            {
                var done = new CountdownEvent(_config.MaxRunningTasksCount);
                Action load = null, sort = null, write = null;

                load = delegate
                {
                    Group x;
                    if (source.TryTake(out x))
                    {
                        using (var reader = groupsReadersPool.Get())
                            do
                            {
                                IUsingHandle<Range> range;
                                if (_linesReservation.TryReserveRange(x.Info.LinesCount, out range))
                                {
                                    IGroupMatrix matrix;
                                    if (_groupMatrixService.TryCreateMatrix(x.Info, out matrix))
                                    {
                                        _groupMatrixService.LoadGroupToMatrix(matrix, x.Info, reader.Value);
                                        loaded.Add(new LoadedGroup(matrix, range, x.PositionInOutput));
                                        continue;
                                    }

                                    range.Dispose();
                                }

                                source.Add(x);
                                break;

                            } while (source.TryTake(out x));

                        _tasksQueue.Enqueue(sort);
                    }
                    else done.Signal();
                };

                sort = delegate
                {
                    LoadedGroup x;
                    while (loaded.TryTake(out x))
                    {
                        _groupSorter.Sort(x.Bytes, x.Lines.Value);
                        sorted.Add(x);
                    }

                    _tasksQueue.Enqueue(write);
                };

                write = delegate
                {
                    LoadedGroup x;
                    using (var writer = resultWritersPool.Get())
                        while (sorted.TryTake(out x))
                            using (x.Lines)
                            using (x.Bytes)
                            {
                                writer.Value.Position = x.PositionInOutput;
                                _sortedGroupWriter.Write(x.Bytes, x.Lines.Value, writer.Value);
                            }

                    _tasksQueue.Enqueue(load);
                };

               for (int i = 0; i < _config.MaxRunningTasksCount; i++)
                    _tasksQueue.Enqueue(load);

                done.Wait();
            }
        }

        private struct Group
        {
            public readonly IGroupInfo Info;
            public readonly long PositionInOutput;

            public Group(
                IGroupInfo info, 
                long positionInOutput)
            {
                Info = info;
                PositionInOutput = positionInOutput;
            }
        }

        private struct LoadedGroup
        {
            public readonly IGroupMatrix Bytes;
            public readonly IUsingHandle<Range> Lines;
            public readonly long PositionInOutput;

            public LoadedGroup(
                IGroupMatrix bytes, 
                IUsingHandle<Range> lines, 
                long positionInOutput)
            {
                Bytes = bytes;
                Lines = lines;
                PositionInOutput = positionInOutput;
            }
        }
    }
}
