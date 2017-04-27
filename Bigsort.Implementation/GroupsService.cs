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
    public class GroupsService
        : IGroupsService
    {
        private const string 
            LogName = nameof(GroupsService),
            MatrixCreationLogName = LogName + "." + nameof(TryCreateGroup),
            GroupLoadingLogName = LogName + "." + nameof(LoadGroup);
         
        private readonly ITimeTracker _timeTracker; 
        private readonly IBuffersPool _buffersPool;
        private readonly IPoolMaker _poolMaker;
        private readonly IIoService _ioService;
        private readonly ITasksQueue _tasksQueue;
        private readonly ILinesReservation _linesReservation;
        private readonly IMemoryOptimizer _memoryOptimizer;
        private readonly int _rowLength;

        public GroupsService(
            IBuffersPool buffersPool,
            ILinesReservation linesReservation,
            IPoolMaker poolMaker,
            IIoService ioService,
            ITasksQueue tasksQueue,
            IMemoryOptimizer memoryOptimizer,
            IConfig config, 
            IDiagnosticTools diagnosticTools = null)
        {
            _buffersPool = buffersPool;
            _linesReservation = linesReservation;
            _poolMaker = poolMaker;
            _ioService = ioService;
            _tasksQueue = tasksQueue;
            _memoryOptimizer = memoryOptimizer;
            _rowLength = config.GroupRowLength;

            _timeTracker = diagnosticTools?.TimeTracker;
        }

        public int RowsCountFor(int bytesCount) =>
            (int) Math.Ceiling((double) bytesCount / _rowLength);

        public IGroup TryCreateGroup(GroupInfo groupInfo)
        {
            var linesRange = _linesReservation
                .TryReserveRange(groupInfo.LinesCount);

            if (linesRange == null)
                return null;

            var rowsCount = RowsCountFor(groupInfo.BytesCount);
            var rows = _buffersPool.TryGetBuffers(rowsCount);
            if (rows == null)
            {
                linesRange.Dispose();
                return null;
            }

            return new Group(_rowLength, groupInfo, linesRange, rows);
        }

        public void LoadGroup(
            IGroup matrix,
            GroupInfo groupInfo,
            IFileReader groupsFileReader)
        {
            int rowIndex = 0, positionInRow = 0;
            var loadingRow = matrix.Rows[rowIndex];
            
            for (int i = 0; i < groupInfo.Mapping.Count; i++)
            {
                var blockRange = groupInfo.Mapping[i];
                groupsFileReader.Position = blockRange.Offset;
                var blockOverPosition = groupsFileReader.Position + blockRange.Length;

                while (groupsFileReader.Position != blockOverPosition)
                {
                    var readLength = Math.Min(
                        (int) (blockOverPosition - groupsFileReader.Position),
                        matrix.RowLength - positionInRow);
                    
                    positionInRow += groupsFileReader
                        .Read(loadingRow, positionInRow, readLength);
                    
                    if (positionInRow == matrix.RowLength 
                        && ++rowIndex != matrix.RowsCount)
                    {
                        loadingRow = matrix.Rows[rowIndex];
                        positionInRow = 0;
                    }
                }
            }
        }

        public IGroupsLoader MakeGroupsLoader(
                string groupFilePath,
                IGroupsSummaryInfo groupsSummary,
                IGroup[] output) =>

            new GroupsLoader(
                groupFilePath,
                groupsSummary,
                output,
                _poolMaker,
                _ioService,
                _tasksQueue,
                _memoryOptimizer,
                _buffersPool,
                _rowLength,
                this,
                _timeTracker);

        private class Group
            : IGroup
        {
            private readonly Action _dispose;

            public Group(
                int rowLength,
                GroupInfo groupInfo,
                IUsingHandle<Range> lines, 
                IUsingHandle<byte[][]> rows)
            {
                BytesCount = groupInfo.BytesCount;
                LinesCount = groupInfo.LinesCount;

                LinesRange = lines.Value;
                Rows = rows.Value;

                RowLength = rowLength;
                RowsCount = Rows.Length;
                
                _dispose = lines.Dispose;
                _dispose += rows.Dispose;
            }

            public byte[][] Rows { get; }
            public Range LinesRange { get; }
            public int LinesCount { get; }
            public int BytesCount { get; }
            public int RowsCount { get; }
            public int RowLength { get; }

            int IReadOnlyCollection<byte>.Count =>
                BytesCount;

            public byte this[int i]
            {
                get { return Rows[i/RowLength][i%RowLength]; }
                set { Rows[i/RowLength][i%RowLength] = value; }
            }

            public IEnumerator<byte> GetEnumerator() =>
                Rows.Select(row => row.Take(RowLength))
                    .Aggregate(Enumerable.Concat)
                    .Take(BytesCount)
                    .GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() =>
                GetEnumerator();

            public void Dispose() =>
                _dispose();
        }

        private class GroupsLoader
            : IGroupsLoader
        {
            public const string
                LogName = nameof(GroupsLoader),
                GroupsLoadingLogName = LogName + "." + nameof(LoadNextGroups);

            private readonly ITimeTracker _timeTracker;
            private readonly IGroupsSummaryInfo _groupsSummary;
            private readonly IGroup[] _output;
            private readonly IPool<IFileReader> _readersPool;
            private readonly ITasksQueue _tasksQueue;
            private readonly IBuffersPool _buffersPool;
            private readonly IGroupsService _service;

            private readonly int[]
                _rowIndexes = new int[Consts.MaxGroupsCount],
                _positionsInRows = new int[Consts.MaxGroupsCount];

            private readonly int _rowLength;

            private int _loadingTop = 0; 

            public GroupsLoader(
                string groupsFilePath,
                IGroupsSummaryInfo groupsSummary,
                IGroup[] output,
                IPoolMaker poolMaker,
                IoService ioService,
                ITasksQueue tasksQueue,
                IMemoryOptimizer memoryOptimizer,
                IBuffersPool buffersPool,
                int rowLength,
                IGroupsService groupService,
                ITimeTracker timeTracker)
            {
                _groupsSummary = groupsSummary;
                _output = output;
                _tasksQueue = tasksQueue;
                _service = groupService;
                _buffersPool = buffersPool;
                _rowLength = rowLength;

                _timeTracker = timeTracker;

                _readersPool = poolMaker.Make(
                    productFactory: () => ioService.OpenRead(groupsFilePath),
                    productDestructor: reader => reader.Dispose());

                memoryOptimizer.OptimizeMemoryForSort(
                    groupsSummary.MaxGroupSize,
                    groupsSummary.MaxGroupLinesCount);
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

                    var group = _output[i] = _service.TryCreateGroup(groupInfo);
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
                        PositionInRow = 0,
                        RowIndex = 0
                    };
                    
                    for (int k = 0; k < mapping.Count; k++, top++)
                    {
                        loading[top] = item;
                        item.PositionInRow += item.Block.Length;
                        item.RowIndex += item.PositionInRow / _rowLength;
                        item.PositionInRow = item.PositionInRow % _rowLength;
                    }
                }

                Array.Sort(loading, BlockLoadingInfo.ByPositionComparer);

                var enginesCount = Math.Max(1, Environment.ProcessorCount - 1);
                var done = new CountdownEvent(enginesCount);
                Action disposeResources = null;

                int loadingBlockIndex = 0;
                for (int j = 0; j < enginesCount; j++)
                {
                    var tempBuffHandle = _buffersPool.GetBuffer();
                    var readerHandle = _readersPool.Get();

                    disposeResources += tempBuffHandle.Dispose;
                    disposeResources += readerHandle.Dispose;

                    var engine = new Engine(
                        tempBuffHandle.Value, 
                        readerHandle.Value, 
                        loading, 
                        _output, 
                        _rowLength);

                    _tasksQueue.Enqueue(() =>
                        engine.Run(done, ref loadingBlockIndex));
                }

                done.Wait();
                disposeResources?.Invoke();

                var offset = _loadingTop;
                _loadingTop = i;

                _timeTracker?.Add(GroupsLoadingLogName, watch.Elapsed);
                return new Range(offset, i - offset);
            }

            public Range LoadNextGroups1()
            {
                var watch = Stopwatch.StartNew();

                var groups = _output;
                var groupsInfos = _groupsSummary.GroupsInfo;
                int[] positionsInRows = _positionsInRows,
                      rowIndexes = _rowIndexes;

                var actualGroupsIndexes = new List<int>();
                int i = _loadingTop, blocksCount = 0;
                for (; i < Consts.MaxGroupsCount; i++)
                {
                    var groupInfo = groupsInfos[i];
                    if (GroupInfo.IsZero(groupInfo))
                        continue;
            
                    var group = _output[i] = _service.TryCreateGroup(groupInfo);
                    if (group == null)
                        break;
                    
                    blocksCount += groupInfo.Mapping.Count;
                    actualGroupsIndexes.Add(i);
                }
                
                var blocks = new LongRange[blocksCount];
                var groupIndexes = new int[blocksCount];
                
                for (int j = 0, top = 0; j < actualGroupsIndexes.Count; j++)
                {
                    var groupIndex = actualGroupsIndexes[j];
                    var mapping = groupsInfos[groupIndex].Mapping;
                    
                    for (int k = 0; k < mapping.Count; k++, top++)
                    {
                        blocks[top] = mapping[k];
                        groupIndexes[top] = groupIndex;
                    }
                }
                
                // Array.Sort(blocks, groupIndexes, LongRange.ByOffsetComparer);

                const int maxFinalActionsCount = 20;
                int finalActionsCount = Math.Min(maxFinalActionsCount, blocksCount),
                    lastActionsIndex = blocksCount - finalActionsCount,
                    rowLength = _rowLength;
                
                var done = new CountdownEvent(finalActionsCount);
                for (int j = 0; j < blocksCount; j++)
                {
                    var groupIndex = groupIndexes[j];
                    var block = blocks[j];

                    var positionInRow = positionsInRows[groupIndex];
                    var rowIndex = rowIndexes[groupIndex];
                    var rows = groups[groupIndex].Rows;

                    var nextPositionInRow = positionsInRows[groupIndex];
                    nextPositionInRow += block.Length;
                    rowIndexes[groupIndex] += nextPositionInRow / rowLength;
                    positionsInRows[groupIndex] = nextPositionInRow % rowLength;

                    var last = j >= lastActionsIndex;
                    
                    _tasksQueue.Enqueue(delegate
                    {
                        using (var readerHandle = _readersPool.Get())
                        {
                            var reader = readerHandle.Value;
                            reader.Position = block.Offset;

                            var rowRightLength = rowLength - positionInRow;
                            if (rowRightLength >= block.Length)
                                reader.Read(rows[rowIndex], positionInRow, block.Length);
                            else
                                using (var tempHandle = _buffersPool.GetBuffer())
                                {
                                    byte[] temp = tempHandle.Value;

                                    reader.Read(temp, 0, block.Length);

                                    Array.Copy(temp, 0,
                                               rows[rowIndex], positionInRow,
                                               rowRightLength);

                                    Array.Copy(temp, rowRightLength,
                                               rows[rowIndex + 1], 0,
                                               block.Length - rowRightLength);
                                }
                        }

                        if (last)
                            done.Signal();
                    });
                }

                done.Wait();

                var offset = _loadingTop;
                _loadingTop = i;

                _timeTracker?.Add(GroupsLoadingLogName, watch.Elapsed);
                return new Range(offset, i - offset);
            }

            private struct BlockLoadingInfo
            {
                public LongRange Block;
                public int GroupIndex, RowIndex, PositionInRow;

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
                private readonly int _rowLength;

                public Engine(
                    byte[] tempBuff, 
                    IFileReader reader, 
                    BlockLoadingInfo[] loadingBloks, 
                    IGroup[] groups,
                    int rowLength)
                {
                    _tempBuff = tempBuff;
                    _reader = reader;
                    _loadingBloks = loadingBloks;
                    _groups = groups;
                    _rowLength = rowLength;
                }

                public void Run(CountdownEvent done, ref int loadingBlockIndex)
                {
                    var groups = _groups;
                    var loadingBlocks = _loadingBloks;
                    var loadingBlocksCount = loadingBlocks.Length;
                    var rowLength = _rowLength;
                    var reader = _reader;
                    var temp = _tempBuff;

                    var i = Interlocked.Increment(ref loadingBlockIndex);
                    while (--i < loadingBlocksCount)
                    {
                        var x = loadingBlocks[i];
                        reader.Position = x.Block.Offset;

                        var rowRightLength = rowLength - x.PositionInRow;
                        if (rowRightLength >= x.Block.Length)
                            reader.Read(groups[x.GroupIndex].Rows[x.RowIndex],
                                        x.PositionInRow,
                                        x.Block.Length);
                        else
                        {
                            var rows = groups[x.GroupIndex].Rows;
                            reader.Read(temp, 0, x.Block.Length);

                            Array.Copy(temp, 0,
                                       rows[x.RowIndex], x.PositionInRow,
                                       rowRightLength);

                            Array.Copy(temp, rowRightLength,
                                       rows[x.RowIndex + 1], 0,
                                       x.Block.Length - rowRightLength);
                        }

                        i = Interlocked.Increment(ref loadingBlockIndex);
                    }

                    done.Signal();
                }
            }

            public Range LoadNextGroups2()
            {
                var groupsInfos = _groupsSummary.GroupsInfo;
                var loading = new Dictionary<int, GroupLoader>();
                var done = new CountdownEvent(0);
                
                int i = _loadingTop, blocksCount = 0;
                for (; i < Consts.MaxGroupsCount; i++)
                {
                    var groupInfo = groupsInfos[i];
                    if (GroupInfo.IsZero(groupInfo))
                        continue;
                    
                    var group = _output[i] = _service.TryCreateGroup(groupInfo);
                    if (group == null)
                        break;

                    blocksCount += groupInfo.Mapping.Count;
                    loading.Add(i, new GroupLoader(groupInfo, group, done,
                                        _readersPool, 
                                        _tasksQueue,
                                        _buffersPool));
                }
                
                done.Reset(loading.Count);
                
                int top = 0;
                var positions = new long[blocksCount];
                var groupIndexes = new int[blocksCount];
                foreach (int j in loading.Keys)
                {
                    var mapping = groupsInfos[j].Mapping;
                    for (int k = 0; k < mapping.Count; k++, top++)
                    {
                        positions[top] = mapping[k].Offset;
                        groupIndexes[top] = j;
                    }
                }

                Array.Sort(positions, groupIndexes);
                for (int j = 0; j < groupIndexes.Length; j++)
                    loading[groupIndexes[j]].EnqueueToLoadNextBlock();
                
                done.Wait();
                
                var offset = _loadingTop;
                _loadingTop = i;

                return new Range(offset, i - offset);
            }

            public void Dispose() =>
                _readersPool.Dispose();
            
            private class GroupLoader
            {
                private readonly IPool<IFileReader> _readersPool;
                private readonly ITasksQueue _tasksQueue;
                private readonly IBuffersPool _buffersPool;
                private readonly CountdownEvent _done;
                private readonly GroupInfo _info;
                private readonly IGroup _group;
                private int
                    _positionInRow = 0,
                    _rowIndex = 0,
                    _block = 0;

                private bool _isOver = false;

                public GroupLoader(
                    GroupInfo info, IGroup group,
                    CountdownEvent done,
                    IPool<IFileReader> readersPool,
                    ITasksQueue tasksQueue, 
                    IBuffersPool buffersPool)
                {
                    _info = info;
                    _group = group;
                    _readersPool = readersPool;
                    _tasksQueue = tasksQueue;
                    _buffersPool = buffersPool;
                    _done = done;
                }

                public void EnqueueToLoadNextBlock()
                {
                    var positionInRow = _positionInRow;
                    var rowIndex = _rowIndex;
                    var blockRange = _info.Mapping[_block];

                    _positionInRow += blockRange.Length;
                    _rowIndex += _positionInRow / _group.RowLength;
                    _positionInRow %= _group.RowLength;
                    _isOver = ++_block == _info.Mapping.Count;

                    var isOver = _isOver;
                    
                    _tasksQueue.Enqueue(delegate
                    {
                        using (var readerHandle = _readersPool.Get())
                        {
                            var reader = readerHandle.Value;
                            reader.Position = blockRange.Offset;

                            var rowRightLength = _group.RowLength - positionInRow;
                            if (rowRightLength >= blockRange.Length)
                                reader.Read(_group.Rows[rowIndex], positionInRow, blockRange.Length);
                            else
                                using (var tempHandle = _buffersPool.GetBuffer())
                                {
                                    byte[] temp = tempHandle.Value;
                                    
                                    reader.Read(temp, 0, blockRange.Length);
                                
                                    Array.Copy(temp, 0,
                                               _group.Rows[rowIndex], positionInRow,
                                               rowRightLength);
                                
                                    Array.Copy(temp, rowRightLength,
                                               _group.Rows[rowIndex + 1], 0,
                                               blockRange.Length - rowRightLength);
                                }
                        }

                        if (isOver)
                            _done.Signal();
                    });
                }
            }
        }
    }
}
