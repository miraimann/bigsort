using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation
{
    public class GroupsLoaderMaker
        : IGroupsLoaderMaker
    {
        private readonly ITimeTracker _timeTracker;
        private readonly IIoServiceMaker _ioServiceMaker;
        private readonly ITasksQueue _tasksQueue;
        private readonly IConfig _config;

        public GroupsLoaderMaker(
            IIoServiceMaker ioServiceMaker,
            ITasksQueue tasksQueue,
            IConfig config,
            IDiagnosticTools diagnosticsTools = null)
        {
            _ioServiceMaker = ioServiceMaker;
            _tasksQueue = tasksQueue;
            _config = config;

            _timeTracker = diagnosticsTools?.TimeTracker;
        }

        public IGroupsLoader Make(
            string groupFilePath, 
            IGroupsSummaryInfo groupsSummary, 
            IGroup[] output,
            LineIndexes[] lines,
            ulong[] sortingSegments,
            IRangablePool<byte[]> buffersPool) =>

            new GroupsLoader(
                groupFilePath,
                groupsSummary,
                output,
                lines,
                sortingSegments,
                buffersPool,
                _ioServiceMaker.Make(buffersPool),
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
            private readonly GroupMaker _groupMaker;

            private readonly IGroupsSummaryInfo _groupsSummary;
            private readonly IRangablePool<byte[]> _buffersPool;
            private readonly IGroup[] _output;

            private readonly Action _dispose;
            private readonly IFileReader[] _readers;
            private readonly byte[][] _tempBuffers;
            private readonly string _groupsFilePath;
            private readonly int 
                _reservedLinesCount,
                _physicalBufferLength,
                _usingBufferLength,
                _enginesCount;

            private int _linesTop = 0;
            private int _loadingTop = 0;

            public GroupsLoader(
                string groupsFilePath,
                IGroupsSummaryInfo groupsSummary,
                IGroup[] output,
                LineIndexes[] lines,
                ulong[] sortingSegments,
                IRangablePool<byte[]> buffersPool,
                IIoService ioService,
                ITasksQueue tasksQueue,
                IConfig config,
                ITimeTracker timeTracker)
            {
                _groupsSummary = groupsSummary;
                _output = output;
                _tasksQueue = tasksQueue;
                _buffersPool = buffersPool;
                _ioService = ioService;

                _groupsFilePath = groupsFilePath;
                _physicalBufferLength = config.PhysicalBufferLength;
                _usingBufferLength = config.UsingBufferLength;

                _reservedLinesCount = lines.Length;
                _groupMaker = new GroupMaker(lines, sortingSegments, _usingBufferLength);
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
                var linesOverIndex = Interlocked
                    .Add(ref _linesTop, groupInfo.LinesCount);
                
                if (linesOverIndex > _reservedLinesCount)
                    return null;

                var buffersCount = BuffersCountFor(groupInfo.BytesCount);
                var buffers = _buffersPool.TryGet(buffersCount);
                if (buffers == null)
                    return null;

                var linesRange = new Range(
                    linesOverIndex - groupInfo.LinesCount, 
                    groupInfo.LinesCount);

                return _groupMaker
                    .Make(groupInfo, linesRange, buffers);
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
                        reader.Position = x.Block.Offset;

                        var buffRightLength = buffLength - x.PositionInBuffer;
                        if (buffRightLength >= x.Block.Length)
                            reader.Read(groups[x.GroupIndex].Buffers[x.BufferIndex],
                                        x.PositionInBuffer,
                                        x.Block.Length);
                        else
                        {
                            var buffers = groups[x.GroupIndex].Buffers;
                            reader.Read(tempBuff, 0, x.Block.Length);

                            Array.Copy(tempBuff, 0,
                                       buffers[x.BufferIndex], x.PositionInBuffer,
                                       buffRightLength);

                            Array.Copy(tempBuff, buffRightLength,
                                       buffers[x.BufferIndex + 1], 0,
                                       x.Block.Length - buffRightLength);
                        }

                        i = Interlocked.Increment(ref loadingBlockIndex);
                    }

                    done.Signal();
                }
            }
        }

        private class GroupMaker
        {
            private readonly LineIndexes[] _linesIndexes;
            private readonly ulong[] _sortingSegments;
            private readonly int _bufferLength;

            public GroupMaker(
                LineIndexes[] linesIndexes, 
                ulong[] sortingSegments, 
                int bufferLength)
            {
                _linesIndexes = linesIndexes;
                _sortingSegments = sortingSegments;
                _bufferLength = bufferLength;
            }


            public IGroup Make(
                    GroupInfo info,
                    Range linesRange,
                    IUsingHandle<byte[][]> buffersHandle) =>

                new Group(
                    info,
                    linesRange,
                    buffersHandle,
                    _bufferLength,
                    _linesIndexes,
                    _sortingSegments);

            private class Group
                : IGroup
            {
                private readonly int _bufferLength;
                private readonly Action _dispose;

                public Group(
                    GroupInfo groupInfo,
                    Range linesRange,
                    IUsingHandle<byte[][]> buffersHandle,
                    int bufferLength,
                    LineIndexes[] linesIndexes,
                    ulong[] sortingSegments)
                {
                    BytesCount = groupInfo.BytesCount;
                    Buffers = buffersHandle.Value;

                    Lines = new ArraySegment<LineIndexes>(
                        linesIndexes,
                        linesRange.Offset,
                        linesRange.Length);

                    SortingSegments = new ArraySegment<ulong>(
                        sortingSegments,
                        linesRange.Offset,
                        linesRange.Length);
                    
                    _bufferLength = bufferLength;
                    _dispose = buffersHandle.Dispose;
                }

                public byte[][] Buffers { get; }
                public ArraySegment<LineIndexes> Lines { get; }
                public ArraySegment<ulong> SortingSegments { get; }
                public int BytesCount { get; }

                int IReadOnlyCollection<byte>.Count =>
                    BytesCount;

                public byte this[int i]
                {
                    get { return Buffers[i / _bufferLength][i % _bufferLength]; }
                    set { Buffers[i / _bufferLength][i % _bufferLength] = value; }
                }

                public IEnumerator<byte> GetEnumerator() =>
                    Buffers.Select(buff => buff.Take(_bufferLength))
                           .Aggregate(Enumerable.Concat)
                           .Take(BytesCount)
                           .GetEnumerator();

                IEnumerator IEnumerable.GetEnumerator() =>
                    GetEnumerator();

                public void Dispose() =>
                    _dispose();
            }
        }
    }
}
