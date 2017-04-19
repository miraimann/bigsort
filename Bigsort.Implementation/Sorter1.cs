//using System;
//using System.Threading;
//using Bigsort.Contracts;

//namespace Bigsort.Implementation
//{
//    public class Sorter1<TSegment>
//        : ISorter
//    {
//        private readonly ILinesReservation<TSegment> _linesReservation;
//        private readonly IPoolMaker _poolMaker;
//        private readonly IGroupMatrixService _groupBytesMatrixService;
//        private readonly IGroupSorter _groupSorter;
//        private readonly ISortedGroupWriter _sortedGroupWriter;
//        private readonly IIoService _ioService;
//        private readonly ITasksQueue _tasksQueue;

//        public Sorter1(
//            ILinesReservation<TSegment> linesReservation,
//            IGroupMatrixService groupBytesMatrixService,
//            IGroupSorter groupSorter,
//            ISortedGroupWriter sortedGroupWriter,
//            IIoService ioService,
//            ITasksQueue tasksQueue,
//            IPoolMaker poolMaker)
//        {
//            _linesReservation = linesReservation;
//            _groupBytesMatrixService = groupBytesMatrixService;
//            _groupSorter = groupSorter;
//            _sortedGroupWriter = sortedGroupWriter;
//            _ioService = ioService;
//            _tasksQueue = tasksQueue;
//            _poolMaker = poolMaker;
//        }

//        public void Sort(
//            string groupsFilePath, 
//            IGroupsSummaryInfo groupsSummary, 
//            string outputPath)
//        {
//            _linesReservation.Load(groupsSummary.MaxGroupLinesCount *
//                                   Environment.ProcessorCount);

//            using (var groupsReadersPool = _poolMaker.Make(
//                               productFactory: () => _ioService.OpenRead(groupsFilePath),
//                            productDestructor: reader => reader.Dispose()))

//            using (var resultWritersPool = _poolMaker.Make(
//                               productFactory: () => _ioService.OpenWrite(outputPath, buffering: true),
//                            productDestructor: writer => writer.Dispose()))
//            {
//                var groupsSorted = new CountdownEvent(Consts.MaxGroupsCount);
//                var possition = 0L;

//                for (int i = 0; i < Consts.MaxGroupsCount; i++)
//                {
//                    var groupInfo = groupsSummary.GroupsInfo[i];
//                    if (groupInfo == null)
//                    {
//                        groupsSorted.Signal();
//                        continue;
//                    }

//                    var rowsInfo = _groupBytesMatrixService
//                        .CalculateRowsInfo(groupInfo.BytesCount);

//                    var groupPosition = possition;
//                    Action sortGroup = null;
//                    sortGroup = () =>
//                    {
//                        IUsingHandle<Range> rangeHandle;
//                        if (_linesReservation.TryReserveRange(groupInfo.LinesCount, out rangeHandle))
//                            using (rangeHandle)
//                            using (var groupsReaderHandle = groupsReadersPool.Get())
//                            using (var resultWriterHandle = resultWritersPool.Get())
//                            using (var group = _groupBytesMatrixService
//                                                    .LoadMatrix(rowsInfo, groupInfo, groupsReaderHandle.Value))
//                            {
//                                var linesRange = rangeHandle.Value;
//                                _groupSorter.Sort(group, linesRange);

//                                resultWriterHandle.Value.Position = groupPosition;
//                                _sortedGroupWriter.Write(group, linesRange, resultWriterHandle.Value);
//                                groupsSorted.Signal();
//                            }
//                        else _tasksQueue.Enqueue(sortGroup);
//                    };

//                    _tasksQueue.Enqueue(sortGroup);
//                    possition += groupInfo.BytesCount;
//                }

//                groupsSorted.Wait();
//            }

//            _ioService.DeleteFile(groupsFilePath);
//        }
//    }
//}
