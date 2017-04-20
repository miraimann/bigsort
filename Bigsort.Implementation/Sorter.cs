using System;
using System.Threading;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class Sorter
        : ISorter
    {
        private readonly ILinesReservation _linesReservation;
        private readonly IPoolMaker _poolMaker;
        private readonly IGroupMatrixService _groupMatrixService;
        private readonly IGroupSorter _groupSorter;
        private readonly ISortedGroupWriter _sortedGroupWriter;
        private readonly IIoService _ioService;
        private readonly ITasksQueue _tasksQueue;

        public Sorter(
            ILinesReservation linesReservation,
            IGroupMatrixService groupMatrixService,
            IGroupSorter groupSorter,
            ISortedGroupWriter sortedGroupWriter,
            IIoService ioService,
            ITasksQueue tasksQueue,
            IPoolMaker poolMaker)
        {
            _linesReservation = linesReservation;
            _groupMatrixService = groupMatrixService;
            _groupSorter = groupSorter;
            _sortedGroupWriter = sortedGroupWriter;
            _ioService = ioService;
            _tasksQueue = tasksQueue;
            _poolMaker = poolMaker;
        }

        public void Sort(
            string groupsFilePath, 
            IGroupsSummaryInfo groupsSummary, 
            string outputPath)
        {
            _linesReservation.Load(groupsSummary.MaxGroupLinesCount *
                                   Environment.ProcessorCount);

            using (var groupsReadersPool = _poolMaker.Make(
                               productFactory: () => _ioService.OpenRead(groupsFilePath),
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
                    
                    var groupPosition = possition;
                    Action sortGroup = null;
                    sortGroup = () =>
                    {
                        IUsingHandle<Range> rangeHandle;
                        if (_linesReservation.TryReserveRange(groupInfo.LinesCount, out rangeHandle))
                            using (rangeHandle)
                            {
                                IGroupMatrix matrix;
                                if (_groupMatrixService.TryCreateMatrix(groupInfo, out matrix))
                                    using (matrix)
                                    using (var reader = groupsReadersPool.Get())
                                    using (var writer = resultWritersPool.Get())
                                    {
                                        _groupMatrixService.LoadGroupToMatrix(matrix, groupInfo, reader.Value);

                                        var linesRange = rangeHandle.Value;
                                        _groupSorter.Sort(matrix, linesRange);

                                        writer.Value.Position = groupPosition;
                                        _sortedGroupWriter.Write(matrix, linesRange, writer.Value);
                                        groupsSorted.Signal();
                                        return;
                                    }
                            }
                        
                        _tasksQueue.Enqueue(sortGroup);
                    };

                    _tasksQueue.Enqueue(sortGroup);
                    possition += groupInfo.BytesCount;
                }

                groupsSorted.Wait();
            }

            _ioService.DeleteFile(groupsFilePath);
        }
    }
}
