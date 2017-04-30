using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation
{
    public class GroupsLoaderMaker
        : IGroupsLoaderMaker
    {
        private readonly ITimeTracker _timeTracker;
        private readonly string _groupsFilePath;
        private readonly IIoService _ioService;
        private readonly ITasksQueue _tasksQueue;
        private readonly IBuffersPool _buffersPool;
        private readonly IConfig _config;

        public GroupsLoaderMaker(
            string groupsFilePath,
            IBuffersPool buffersPool,
            IIoService ioService,
            ITasksQueue tasksQueue,
            IConfig config,
            IDiagnosticTools diagnosticsTools = null)
        {

            _groupsFilePath = groupsFilePath;
            _buffersPool = buffersPool;
            _ioService = ioService;
            _tasksQueue = tasksQueue;
            _config = config;

            _timeTracker = diagnosticsTools?.TimeTracker;
        }

        public IGroupsLoader Make(IGroupsSummaryInfo groupsSummary, IGroup[] output) =>
            new GroupsLoader(
                _groupsFilePath,
                groupsSummary,
                output,
                _buffersPool,
                _ioService,
                _tasksQueue,
                _config,
                _timeTracker);
        
        private class GroupsLoader
            : IGroupsLoader
        {
            public const string
                LogName = nameof(GroupsLoader),
                GroupsLoadingLogName = LogName + "." + nameof(LoadNextGroups);

            private readonly ITimeTracker _timeTracker;

            private readonly IIoService _ioService;
            private readonly ITasksQueue _tasksQueue;

            private readonly IGroupsSummaryInfo _groupsSummary;
            private readonly IGroup[] _output;
            private readonly LineIndexes[] _linesIndexes;
            private readonly ulong[] _sortingSegments;
            private readonly IBuffersPool _buffersPool;

            private readonly Action _dispose;
            private readonly IFileReader[] _readers;
            private readonly byte[][] _tempBuffers, _buffers;
            private readonly string _groupsFilePath;
            private readonly int 
                _reservedLinesCount,
                _physicalBufferLength,
                _usingBufferLength,
                _enginesCount;

            private int _linesTop, _loadingTop, _buffersTop;

            public GroupsLoader(
                string groupsFilePath,
                IGroupsSummaryInfo groupsSummary,
                IGroup[] output,
                IBuffersPool buffersPool,
                IIoService ioService,
                ITasksQueue tasksQueue,
                IConfig config,
                ITimeTracker timeTracker)
            {
                _groupsSummary = groupsSummary;
                _output = output;
                _tasksQueue = tasksQueue;
                _ioService = ioService;
                _buffersPool = buffersPool;

                var memoryUsedForBuffers = (long) 
                    _buffersPool.Count * 
                    config.PhysicalBufferLength;
                
                var maxGroupBuffersCount = (int) Math.Ceiling((double)
                    groupsSummary.MaxGroupSize / 
                    config.PhysicalBufferLength);

                var maxGroupSize = 
                    maxGroupBuffersCount * 
                    config.PhysicalBufferLength;

                var lineSize = 
                    Marshal.SizeOf<LineIndexes>() +
                    sizeof(ulong);

                var maxSizeForGroupLines = 
                    lineSize * 
                    groupsSummary.MaxGroupLinesCount;

                var maxLoadedGroupsCount = 
                    memoryUsedForBuffers / 
                    (maxGroupSize + maxSizeForGroupLines);

                var memoryForLines = (int)
                    maxLoadedGroupsCount * 
                    maxSizeForGroupLines;

                _reservedLinesCount = 
                    memoryForLines / 
                    lineSize;

                var buffersCountForFree = (int) Math.Ceiling((double) 
                    memoryForLines / 
                    config.PhysicalBufferLength);

                var buffersCount = 
                    _buffersPool.Count - 
                    buffersCountForFree;

                var allBuffers = _buffersPool.ExtractAll();
                Array.Resize(ref allBuffers, buffersCount);
                
                _buffers = allBuffers;
                _linesIndexes = new LineIndexes[_reservedLinesCount];
                _sortingSegments = new ulong[_reservedLinesCount];
                
                _groupsFilePath = groupsFilePath;
                _physicalBufferLength = config.PhysicalBufferLength;
                _usingBufferLength = config.UsingBufferLength;

                _enginesCount = Math.Max(1, Environment.ProcessorCount - 1);

                _readers = Enumerable
                    .Range(0, _enginesCount)
                    .Select(_ => _ioService.OpenRead(_groupsFilePath))
                    .ToArray();

                var tempBuffersHandles = Enumerable
                    .Range(0, _enginesCount)
                    .Select(_ => _buffersPool.Get())
                    .ToArray();

                _tempBuffers = tempBuffersHandles
                    .Select(o => o.Value)
                    .ToArray();

                _dispose = Enumerable
                    .Concat<IDisposable>(_readers, tempBuffersHandles)
                    .Select(o => new Action(o.Dispose))
                    .Aggregate((a, b) => a + b);

                _timeTracker = timeTracker;
            }

            public Range LoadNextGroups()
            {
                var watch = Stopwatch.StartNew();

                var groupsInfos = _groupsSummary.GroupsInfo;
                var actualGroupsIndexes = new List<int>();

                int i = _loadingTop, blocksCount = 0;
                for (; i < Consts.MaxGroupsCount; i++)
                {
                    var groupInfo = groupsInfos[i];
                    if (GroupInfo.IsZero(groupInfo))
                        continue;

                    var group = _output[i] = TryCreateGroup(groupInfo);
                    if (group == null)
                        break;

                    blocksCount += groupInfo.Mapping.Count;
                    actualGroupsIndexes.Add(i);
                }

                var loading = new BlockLoadingInfo[blocksCount];
                for (int j = 0, top = 0; j < actualGroupsIndexes.Count; j++)
                {
                    var groupIndex = actualGroupsIndexes[j];
                    var mapping = groupsInfos[groupIndex].Mapping;
                    var item = new BlockLoadingInfo
                    {
                        GroupIndex = groupIndex,
                        Block = mapping[0],
                        PositionInBuffer = 0,
                        BufferIndex = 0
                    };

                    for (int k = 0; k < mapping.Count; k++, top++)
                    {
                        loading[top] = item;
                        item.PositionInBuffer += item.Block.Length;
                        item.BufferIndex += item.PositionInBuffer / _usingBufferLength;
                        item.PositionInBuffer = item.PositionInBuffer % _usingBufferLength;
                    }
                }

                Array.Sort(loading, BlockLoadingInfo.ByPositionComparer);
                
                var done = new CountdownEvent(_enginesCount);
                int loadingBlockIndex = 0;
                
                for (int j = 0; j < _enginesCount; j++)
                {   
                    var engine = new Engine(
                        _tempBuffers[j],
                        _readers[j],
                        loading,
                        _output,
                        _usingBufferLength);

                    _tasksQueue.Enqueue(() =>
                        engine.Run(done, ref loadingBlockIndex));
                }

                done.Wait();

                var offset = _loadingTop;
                _loadingTop = i;

                _timeTracker?.Add(GroupsLoadingLogName, watch.Elapsed);
                return new Range(offset, i - offset);
            }

            private int BuffersCountFor(int bytesCount) =>
                (int) Math.Ceiling((double) bytesCount / _physicalBufferLength);

            private IGroup TryCreateGroup(GroupInfo groupInfo)
            {
                var linesOverIndex = _linesTop + groupInfo.LinesCount;
                if (linesOverIndex > _reservedLinesCount)
                    return null;

                var buffersCount = BuffersCountFor(groupInfo.BytesCount);
                var buffersOverIndex = _buffersTop + buffersCount;
                if (buffersOverIndex > _buffers.Length)
                    return null;
                
                var buffersRange = new Range(_buffersTop, buffersCount);
                var linesRange = new Range(_linesTop, groupInfo.LinesCount);
                var group = new Group
                {
                    BytesCount = groupInfo.BytesCount,
                    Buffers = CreateArraySegment(_buffers, buffersRange),
                    Lines = CreateArraySegment(_linesIndexes, linesRange),
                    SortingSegments = CreateArraySegment(_sortingSegments, linesRange)
                };

                _linesTop = linesOverIndex;
                _buffersTop = buffersOverIndex;

                return group;
            }
            
            public void Dispose() =>
                _dispose();

            private struct BlockLoadingInfo
            {
                public LongRange Block;
                public int GroupIndex, BufferIndex, PositionInBuffer;

                public static readonly IComparer<BlockLoadingInfo> ByPositionComparer;

                static BlockLoadingInfo()
                {
                    var longComparer = Comparer<long>.Default;
                    ByPositionComparer = Comparer<BlockLoadingInfo>.Create(
                        (a, b) => longComparer.Compare(a.Block.Offset, b.Block.Offset));
                }
            }

            private class Engine
            {
                private readonly byte[] _tempBuff;
                private readonly IFileReader _reader;
                private readonly BlockLoadingInfo[] _loadingBloks;
                private readonly IGroup[] _groups;
                private readonly int _usingBufferLength;

                public Engine(
                    byte[] tempBuff,
                    IFileReader reader,
                    BlockLoadingInfo[] loadingBloks,
                    IGroup[] groups,
                    int usingBufferLength)
                {
                    _tempBuff = tempBuff;
                    _reader = reader;
                    _loadingBloks = loadingBloks;
                    _groups = groups;
                    _usingBufferLength = usingBufferLength;
                }

                public void Run(CountdownEvent done, ref int loadingBlockIndex)
                {
                    var groups = _groups;
                    var loadingBlocks = _loadingBloks;
                    var loadingBlocksCount = loadingBlocks.Length;
                    var buffLength = _usingBufferLength;
                    var reader = _reader;
                    var tempBuff = _tempBuff;

                    var i = Interlocked.Increment(ref loadingBlockIndex);
                    while (--i < loadingBlocksCount)
                    {
                        var x = loadingBlocks[i];
                        var buffers = groups[x.GroupIndex].Buffers;
                        reader.Position = x.Block.Offset;

                        var buffRightLength = buffLength - x.PositionInBuffer;
                        if (buffRightLength >= x.Block.Length)
                            reader.Read(buffers.Array[buffers.Offset + x.BufferIndex],
                                        x.PositionInBuffer,
                                        x.Block.Length);
                        else
                        {
                            reader.Read(tempBuff, 0, x.Block.Length);

                            Array.Copy(tempBuff, 0,
                                       buffers.Array[buffers.Offset + x.BufferIndex], x.PositionInBuffer,
                                       buffRightLength);

                            Array.Copy(tempBuff, buffRightLength,
                                       buffers.Array[buffers.Offset + x.BufferIndex + 1], 0,
                                       x.Block.Length - buffRightLength);
                        }

                        i = Interlocked.Increment(ref loadingBlockIndex);
                    }

                    done.Signal();
                }
            }
        }

        private static ArraySegment<T> CreateArraySegment<T>(T[] array, Range range) =>
            new ArraySegment<T>(array, range.Offset, range.Length);

        private class Group
            : IGroup
        {
            public ArraySegment<byte[]> Buffers { get; set; }
            public ArraySegment<LineIndexes> Lines { get; set; }
            public ArraySegment<ulong> SortingSegments { get; set; }
            public int BytesCount { get; set; }
        }
    }
}
