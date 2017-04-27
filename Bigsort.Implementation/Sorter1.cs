using System.Threading;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class Sorter1
        : ISorter
    {
        private readonly IGroupsService _groupsService;
        private readonly IGroupSorter _groupSorter;
        private readonly ISortedGroupWriterMaker _sortedGroupWriterMaker;
        private readonly ITasksQueue _tasksQueue;
        
        public Sorter1(
            IGroupsService groupsService,
            IGroupSorter groupSorter,
            ISortedGroupWriterMaker sortedGroupWriterMaker,
            ITasksQueue tasksQueue)
        {
            _groupsService = groupsService;
            _groupSorter = groupSorter;
            _sortedGroupWriterMaker = sortedGroupWriterMaker;
            _tasksQueue = tasksQueue;
        }

        public void Sort(
            string groupsFilePath,
            IGroupsSummaryInfo groupsSummary,
            string outputPath)
        {
            var groups = new IGroup[Consts.MaxGroupsCount];

            using (var groupsWriter = _sortedGroupWriterMaker.Make(outputPath))
            using (var groupsLoader = _groupsService
                        .MakeGroupsLoader(groupsFilePath, groupsSummary, groups))
            {
                var loadedGroupsRange = groupsLoader.LoadNextGroups();
                while (!Range.IsZero(loadedGroupsRange))
                {
                    var groupsBlockDone = new CountdownEvent(loadedGroupsRange.Length);
                    int i = loadedGroupsRange.Offset,
                        n = i + loadedGroupsRange.Length;
                    
                    while (i != n)
                    {
                        var j = i++;
                        var group = groups[j];
                        if (group == null)
                        {
                            groupsBlockDone.Signal();
                            continue;
                        }

                        _tasksQueue.Enqueue(delegate
                        {
                            _groupSorter.Sort(group);
                            groupsBlockDone.Signal();
                        });
                    }

                    groupsBlockDone.Wait();
                    groupsBlockDone.Reset();

                    var possition = 0L;
                    i = loadedGroupsRange.Offset;
                    while (i != n)
                    {
                        var j = i++;
                        var p = possition;
                        var group = groups[j];
                        if (group == null)
                        {
                            groupsBlockDone.Signal();
                            continue;
                        }
                        
                        possition += group.BytesCount;
                        _tasksQueue.Enqueue(delegate
                        {
                            using (group)
                            {
                                groupsWriter.Write(group, p);
                                groups[j] = null;
                                groupsBlockDone.Signal();
                            }
                        });
                    }

                    groupsBlockDone.Wait();
                    loadedGroupsRange = groupsLoader.LoadNextGroups();
                }
            }
        }
    }
}
