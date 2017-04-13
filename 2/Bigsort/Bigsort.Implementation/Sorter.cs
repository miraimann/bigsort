using Bigsort.Contracts;
using System;
using System.IO;
using System.Threading;
using System.Linq;

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
            ITasksQueueMaker tasksQueueMaker,
            IPoolMaker poolMaker,
            IConfig config)
        {
            _linesReservation = linesReservation;
            _grouper = grouper;
            _groupBytesMatrixService = groupBytesMatrixService;
            _groupSorter = groupSorter;
            _sortedGroupWriter = sortedGroupWriter;
            _ioService = ioService;
            _poolMaker = poolMaker;
            _config = config;

            _tasksQueue = tasksQueueMaker
                .MakeQueue(Environment.ProcessorCount);
        }

        public void Sort(string inputPath, string outputPath)
        {
            var groupsSummary = _grouper.SplitToGroups(inputPath);
            var fileLength = _ioService.SizeOfFile(inputPath);

            _ioService.CreateFile(outputPath, fileLength);
            _linesReservation.Load(groupsSummary.MaxGroupLinesCount *
                                   Environment.ProcessorCount);

            using (var s = File.OpenWrite("E:\\log.txt"))
            using (var w = new StreamWriter(s))
            using (var groupsReadersPool = _poolMaker.Make(
                               productFactory: () => _ioService.OpenRead(_config.GroupsFilePath),
                            productDestructor: reader => reader.Dispose()))

            using (var resultWritersPool = _poolMaker.Make(
                               productFactory: () => _ioService.OpenWrite(outputPath),
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

                    w.Write($"{i:00000000}:{groupPosition:00000000}|"); 

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

            _ioService.DeleteFile(_config.GroupsFilePath);
        }
    }
}
