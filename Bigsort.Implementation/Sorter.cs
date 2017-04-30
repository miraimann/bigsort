using System;
using System.Runtime.InteropServices;
using System.Threading;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class SorterMaker
        : ISorterMaker
    {
        private readonly IGroupsLoaderMaker _groupsLoaderMaker;
        private readonly IGroupSorter _groupSorter;
        private readonly ISortedGroupWriterMaker _groupWriterMaker;
        private readonly ITasksQueue _tasksQueue;
        private readonly IPoolMaker _poolMaker;
        private readonly IConfig _config;

        public SorterMaker(
            IGroupsLoaderMaker groupsLoaderMaker, 
            IGroupSorter groupSorter, 
            ISortedGroupWriterMaker groupWriterMaker, 
            ITasksQueue tasksQueue,
            IPoolMaker poolMaker,
            IConfig config)
        {
            _groupsLoaderMaker = groupsLoaderMaker;
            _groupSorter = groupSorter;
            _groupWriterMaker = groupWriterMaker;
            _tasksQueue = tasksQueue;
            _poolMaker = poolMaker;
            _config = config;
        }

        public ISorter Make(IPool<byte[]> buffersPool) =>
            new Sorter(
                buffersPool,
                _groupsLoaderMaker,
                _groupSorter,
                _groupWriterMaker,
                _tasksQueue,
                _poolMaker,
                _config);

        private class Sorter
            : ISorter
        {
            private readonly IGroupsLoaderMaker _groupsLoaderMaker;
            private readonly IGroupSorter _groupSorter;
            private readonly ISortedGroupWriterMaker _groupWriterMaker;
            private readonly ITasksQueue _tasksQueue;
            private readonly IPoolMaker _poolMaker;
            private readonly IPool<byte[]> _buffersPool;
            private readonly IConfig _config;

            public Sorter(
                IPool<byte[]> buffersPool,
                IGroupsLoaderMaker groupsLoaderMaker,
                IGroupSorter groupSorter,
                ISortedGroupWriterMaker sortedGroupWriterMaker,
                ITasksQueue tasksQueue,
                IPoolMaker poolMaker,
                IConfig config)
            {
                _groupsLoaderMaker = groupsLoaderMaker;
                _groupSorter = groupSorter;
                _groupWriterMaker = sortedGroupWriterMaker;
                _tasksQueue = tasksQueue;
                _buffersPool = buffersPool;
                _config = config;
                _poolMaker = poolMaker;
            }

            public void Sort(
                string groupsFilePath,
                IGroupsSummaryInfo groupsSummary,
                string outputPath)
            {
                var memoryUsedForBuffers = (long) _buffersPool.Count * _config.PhysicalBufferLength;
                var maxGroupBuffersCount = (int) Math.Ceiling((double) groupsSummary.MaxGroupSize / _config.PhysicalBufferLength);
                var maxGroupSize = maxGroupBuffersCount * _config.PhysicalBufferLength;

                var lineSize = Marshal.SizeOf<LineIndexes>() + sizeof(ulong);
                var maxSizeForGroupLines = lineSize * groupsSummary.MaxGroupLinesCount;
                var maxLoadedGroupsCount = memoryUsedForBuffers / (maxGroupSize + maxSizeForGroupLines);

                var memoryForLines = (int) maxLoadedGroupsCount * maxSizeForGroupLines;
                var linesCountForReserve = memoryForLines / lineSize;
                var buffersCountForFree = (int)Math.Ceiling((double)memoryForLines / _config.PhysicalBufferLength);
                var buffersCount = _buffersPool.Count - buffersCountForFree;

                var allBuffers = _buffersPool.ExtractAll();
                Array.Resize(ref allBuffers, buffersCount);

                var rangableBuffersPool = _poolMaker.MakeRangablePool(allBuffers,
                    () => new byte[_config.PhysicalBufferLength]);

                var lines = new LineIndexes[linesCountForReserve];
                var sortingSegments = new ulong[linesCountForReserve];
                var groups = new IGroup[Consts.MaxGroupsCount];
                
                using (var groupsWriter = _groupWriterMaker.Make(outputPath, _buffersPool))
                using (var groupsLoader = _groupsLoaderMaker
                                .Make(groupsFilePath, groupsSummary, groups, lines, 
                                      sortingSegments, rangableBuffersPool))
                {
                    var loadedGroupsRange = groupsLoader.LoadNextGroups();
                    while (!Range.IsZero(loadedGroupsRange))
                    {
                        var groupsBlockDone = new CountdownEvent(loadedGroupsRange.Length);
                        int i = loadedGroupsRange.Offset,
                            n = i + loadedGroupsRange.Length;

                        while (i != n)
                        {
                            var j = i++;
                            var group = groups[j];
                            if (group == null)
                            {
                                groupsBlockDone.Signal();
                                continue;
                            }

                            _tasksQueue.Enqueue(delegate
                            {
                                _groupSorter.Sort(group);
                                groupsBlockDone.Signal();
                            });
                        }

                        groupsBlockDone.Wait();
                        groupsBlockDone.Reset();

                        var possition = 0L;
                        i = loadedGroupsRange.Offset;
                        while (i != n)
                        {
                            var j = i++;
                            var p = possition;
                            var group = groups[j];
                            if (group == null)
                            {
                                groupsBlockDone.Signal();
                                continue;
                            }

                            possition += group.BytesCount;
                            _tasksQueue.Enqueue(delegate
                            {
                                using (group)
                                {
                                    groupsWriter.Write(group, p);
                                    groups[j] = null;
                                    groupsBlockDone.Signal();
                                }
                            });
                        }

                        groupsBlockDone.Wait();
                        loadedGroupsRange = groupsLoader.LoadNextGroups();
                    }
                }
            }
        }
    }
}
