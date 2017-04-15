using Bigsort.Contracts;
using System;
using System.Threading;

namespace Bigsort.Implementation
{
    public class Sorter<TSegment>
        : ISorter
    {
        private readonly ILinesReservation<TSegment> _linesReservation; 
        private readonly IGrouper _grouper;
        private readonly IPoolMaker _poolMaker;
        private readonly IGroupBytesMatrixService _groupBytesMatrixService;
        private readonly IGroupSorter _groupSorter;
        private readonly ISortedGroupWriter _sortedGroupWriter;
        private readonly IIoService _ioService;
        private readonly ITasksQueue _tasksQueue;
        private readonly IConfig _config;

        public Sorter(
            ILinesReservation<TSegment> linesReservation,
            IGrouper grouper,
            IGroupBytesMatrixService groupBytesMatrixService,
            IGroupSorter groupSorter,
            ISortedGroupWriter sortedGroupWriter,
            IIoService ioService,
            ITasksQueue tasksQueue,
            IPoolMaker poolMaker,
            IConfig config)
        {
            _linesReservation = linesReservation;
            _grouper = grouper;
            _groupBytesMatrixService = groupBytesMatrixService;
            _groupSorter = groupSorter;
            _sortedGroupWriter = sortedGroupWriter;
            _ioService = ioService;
            _tasksQueue = tasksQueue;
            _poolMaker = poolMaker;
            _config = config;
        }

        public void Sort(string inputPath, string outputPath)
        {
            var fileLength = _ioService.SizeOfFile(inputPath);
            var groupsFile = _ioService.CreateTempFile(fileLength);
            var groupsSummary = _grouper.SplitToGroups(inputPath, groupsFile);
            
            _ioService.CreateFile(outputPath, fileLength);
            _linesReservation.Load(groupsSummary.MaxGroupLinesCount *
                                   Environment.ProcessorCount);

            using (var groupsReadersPool = _poolMaker.Make(
                               productFactory: () => _ioService.OpenRead(groupsFile),
                            productDestructor: reader => reader.Dispose()))

            using (var resultWritersPool = _poolMaker.Make(
                               productFactory: () => _ioService.OpenWrite(outputPath, buffering: true),
                            productDestructor: writer => writer.Dispose()))
            {
                var groupsSorted = new CountdownEvent(Consts.MaxGroupsCount);
                var possition = 0L;

                for (int i = 0; i < Consts.MaxGroupsCount; i++)
                {
                    var groupInfo = groupsSummary.GroupsInfo[i];
                    if (groupInfo == null)
                    {
                        groupsSorted.Signal();
                        continue;
                    }

                    var rowsInfo = _groupBytesMatrixService
                        .CalculateRowsInfo(groupInfo.BytesCount);

                    var groupPosition = possition;
                    Action sortGroup = null;
                    sortGroup = () =>
                    {
                        IUsingHandle<Range> rangeHandle;
                        if (_linesReservation.TryReserveRange(groupInfo.LinesCount, out rangeHandle))
                            using (rangeHandle)
                            using (var groupsReaderHandle = groupsReadersPool.Get())
                            using (var resultWriterHandle = resultWritersPool.Get())
                            using (var group = _groupBytesMatrixService
                                                    .LoadMatrix(rowsInfo, groupInfo, groupsReaderHandle.Value))
                            {
                                var linesRange = rangeHandle.Value;
                                _groupSorter.Sort(group, linesRange);

                                resultWriterHandle.Value.Position = groupPosition;
                                _sortedGroupWriter.Write(group, linesRange, resultWriterHandle.Value);
                                groupsSorted.Signal();
                            }
                        else _tasksQueue.Enqueue(sortGroup);
                    };

                    _tasksQueue.Enqueue(sortGroup);
                    possition += groupInfo.BytesCount;
                }

                groupsSorted.Wait();
            }

            _ioService.DeleteFile(groupsFile);
        }
    }
}
