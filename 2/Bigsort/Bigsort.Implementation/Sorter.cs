using Bigsort.Contracts;
using System;
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
                .MakeQueue(1);
                //.MakeQueue(Environment.ProcessorCount);
        }

        public void Sort(string inputPath, string outputPath)
        {
            var groupsSummary = _grouper.SplitToGroups(inputPath);
            var fileLength = _ioService.SizeOfFile(inputPath);

            var dbg = groupsSummary
                .GroupsInfo
                .Where(o => o != null)
                .Sum(o => o.BytesCount);

            // using (var rdr = _ioService.OpenRead(_config.GroupsFilePath))
            // {
            //     var buff = new byte[1024];
            //     rdr.Position = 102553;
            //     var lng = rdr.Read(buff, 0, 96);
            // }
            
            _ioService.CreateFile(outputPath, fileLength);
            _linesReservation.Load(groupsSummary.MaxGroupLinesCount *
                                   Environment.ProcessorCount);

            using (var groupsFileReadersPool = _poolMaker.Make(
                                  productFactory: () => _ioService.OpenRead(_config.GroupsFilePath),
                               productDestructor: reader => reader.Dispose()))

            using (var resultFileWritersPool = _poolMaker.Make(
                                  productFactory: () => _ioService.OpenWrite(outputPath),
                               productDestructor: writer => writer.Dispose()))
            {
                // var groupsSorted = new CountdownEvent(1);
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
                    var dbg_i = i;
                    sortGroup = () =>
                    {
                        var dbgg_i = dbg_i;
                        IUsingHandle<Range> rangeHandle;
                        if (_linesReservation.TryReserveRange(groupInfo.LinesCount, out rangeHandle))
                            using (rangeHandle)
                            using (var groupsFileReaderHandle = groupsFileReadersPool.Get())
                            using (var resultFileWriterHandle = resultFileWritersPool.Get())
                            using (var group = _groupBytesMatrixService.LoadMatrix(rowsInfo, groupInfo,
                                                    groupsFileReaderHandle.Value))
                            {
                                var linesRange = rangeHandle.Value;
                                _groupSorter.Sort(group, linesRange);

                                resultFileWriterHandle.Value.Position = groupPosition;
                                _sortedGroupWriter.Write(group, linesRange,
                                    resultFileWriterHandle.Value);
                                groupsSorted.Signal();
                            }
                        else _tasksQueue.Enqueue(sortGroup);
                    };

                    _tasksQueue.Enqueue(sortGroup);
                    //break;
                    possition += groupInfo.BytesCount;
                }

                groupsSorted.Wait();
            }

            _ioService.DeleteFile(_config.GroupsFilePath);
        }
    }
}
