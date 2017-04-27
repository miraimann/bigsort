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
        private readonly IGroupsService _groupMatrixService;
        private readonly IGroupSorter _groupSorter;
        private readonly ISortedGroupWriterMaker _sortedGroupWriterMaker;
        private readonly IIoService _ioService;
        private readonly ITasksQueue _tasksQueue;

        public Sorter(
            ILinesReservation linesReservation,
            IGroupsService groupMatrixService,
            IGroupSorter groupSorter,
            ISortedGroupWriterMaker sortedGroupWriterMaker,
            IIoService ioService,
            ITasksQueue tasksQueue,
            IPoolMaker poolMaker)
        {
            _linesReservation = linesReservation;
            _groupMatrixService = groupMatrixService;
            _groupSorter = groupSorter;
            _sortedGroupWriterMaker = sortedGroupWriterMaker;
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

            using (var sortedGroupWriter = _sortedGroupWriterMaker.Make(outputPath))
            using (var groupsReadersPool = _poolMaker.Make(
                               productFactory: () => _ioService.OpenRead(groupsFilePath),
                            productDestructor: reader => reader.Dispose()))
            {
                var groupsSorted = new CountdownEvent(Consts.MaxGroupsCount);
                var possition = 0L;

                for (int i = 0; i < Consts.MaxGroupsCount; i++)
                {
                    var groupInfo = groupsSummary.GroupsInfo[i];
                    if (GroupInfo.IsZero(groupInfo))
                    {
                        groupsSorted.Signal();
                        continue;
                    }
                    
                    var groupPosition = possition;
                    Action sortGroup = null;
                    sortGroup = () =>
                    {
                        
                        var group = _groupMatrixService.TryCreateGroup(groupInfo);
                        if (group != null)
                            using (group)
                            using (var reader = groupsReadersPool.Get())
                            {
                                _groupMatrixService.LoadGroup(group, groupInfo, reader.Value);
                                _groupSorter.Sort(group);
                                sortedGroupWriter.Write(group, groupPosition);
                                groupsSorted.Signal();
                                return;
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
