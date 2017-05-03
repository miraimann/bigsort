using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly IBuffersPool _buffersPool;
        private readonly IConfig _config;

        public GroupsLoaderMaker(
            string groupsFilePath,
            IBuffersPool buffersPool,
            IIoService ioService,
            IConfig config,
            IDiagnosticTools diagnosticsTools = null)
        {
            _groupsFilePath = groupsFilePath;
            _buffersPool = buffersPool;
            _ioService = ioService;
            _config = config;

            _timeTracker = diagnosticsTools?.TimeTracker;
        }

        public IGroupsLoader Make(GroupInfo[] groupsInfo, IGroup[] output) =>
            new GroupsLoader(
                _groupsFilePath,
                groupsInfo,
                output,
                _buffersPool,
                _ioService,
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
            private readonly IBuffersPool _buffersPool;

            private readonly GroupInfo[] _groupsInfo;
            private readonly IGroup[] _output;
            private readonly LineIndexes[] _linesIndexes;
            private readonly ulong[] _sortingSegments;
            private readonly byte[][] _tempBuffers, _buffers;

            private readonly Action _dispose;
            private readonly IFileReader[] _readers;
            private readonly string _groupsFilePath;
            private readonly int 
                _reservedLinesCount,
                _usingBufferLength;

            private int _linesTop, _loadingTop, _buffersTop;

            public GroupsLoader(
                string groupsFilePath,
                GroupInfo[] groupsInfo,
                IGroup[] output,
                IBuffersPool buffersPool,
                IIoService ioService,
                IConfig config,
                ITimeTracker timeTracker)
            {
                _groupsInfo = groupsInfo;
                _output = output;
                _ioService = ioService;
                _buffersPool = buffersPool;

                int maxGroupBytesCount = 0, maxGroupLinesCount = 0;
                for (int i = 0; i < Consts.MaxGroupsCount; i++)
                {
                    var info = groupsInfo[i];
                    if (!GroupInfo.IsZero(info))
                    {
                        maxGroupBytesCount = Math.Max(maxGroupBytesCount, info.BytesCount);
                        maxGroupLinesCount = Math.Max(maxGroupLinesCount, info.LinesCount);
                    }
                }

                var memoryUsedForBuffers = (long) 
                    _buffersPool.Count * 
                    config.PhysicalBufferLength;
                
                var maxGroupBuffersCount = (int) Math.Ceiling((double)
                    maxGroupBytesCount / 
                    config.UsingBufferLength);

                var maxGroupSize = 
                    maxGroupBuffersCount * 
                    config.PhysicalBufferLength;

                var lineSize = 
                    Marshal.SizeOf<LineIndexes>() +
                    sizeof(ulong);

                var maxSizeForGroupLines = 
                    lineSize * 
                    maxGroupLinesCount;

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
                _usingBufferLength = config.UsingBufferLength;
                
                _readers = Enumerable
                    .Range(0, Consts.MaxRunningTasksCount)
                    .Select(_ => _ioService.OpenRead(_groupsFilePath))
                    .ToArray();

                var tempBuffersHandles = Enumerable
                    .Range(0, Consts.MaxRunningTasksCount)
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

                _linesTop = 0; 
                _buffersTop = 0;

                var groupsInfos = _groupsInfo;
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
                        PositionInBuffer = 0,
                        BufferIndex = 0
                    };
                    
                    for (int k = 0; k < mapping.Count; k++, top++)
                    {
                        item.Block = mapping[k];
                        loading[top] = item;

                        item.PositionInBuffer += item.Block.Length;
                        item.BufferIndex += item.PositionInBuffer / _usingBufferLength;
                        item.PositionInBuffer = item.PositionInBuffer % _usingBufferLength;
                    }
                }

                Array.Sort(loading, BlockLoadingInfo.ByPositionComparer);
                Parallel.ForEach(
                    Enumerable.Range(0, Consts.MaxRunningTasksCount), 
                    Consts.UseMaxTasksCountOptions,
                    j =>
                    {
                        var groups = _output;
                        var buffLength = _usingBufferLength;
                        var reader = _readers[j];
                        var tempBuff = _tempBuffers[j];

                        for (; j < loading.Length; j += Consts.MaxRunningTasksCount)
                        {
                            var x = loading[j];
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
                        }
                    });

                var offset = _loadingTop;
                _loadingTop = i;

                _timeTracker?.Add(GroupsLoadingLogName, watch.Elapsed);
                return new Range(offset, i - offset);
            }

            private int BuffersCountFor(int bytesCount) =>
                (int) Math.Ceiling((double) bytesCount / _usingBufferLength);

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
