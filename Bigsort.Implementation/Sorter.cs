using System.Threading;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class Sorter
        : ISorter
    {
        private readonly IGroupsLoaderMaker _groupsLoaderMaker;
        private readonly IGroupSorter _groupSorter;
        private readonly ISortedGroupWriterMaker _groupWriterMaker;
        private readonly ITasksQueue _tasksQueue;
        
        public Sorter(
            IGroupsLoaderMaker groupsLoaderMaker,
            IGroupSorter groupSorter,
            ISortedGroupWriterMaker sortedGroupWriterMaker,
            ITasksQueue tasksQueue)
        {
            _groupsLoaderMaker = groupsLoaderMaker;
            _groupSorter = groupSorter;
            _groupWriterMaker = sortedGroupWriterMaker;
            _tasksQueue = tasksQueue;
        }

        public void Sort(
            string groupsFilePath,
            IGroupsSummaryInfo groupsSummary,
            string outputPath)
        {
            var groups = new IGroup[Consts.MaxGroupsCount];

            using (var groupsWriter = _groupWriterMaker.Make(outputPath))
            using (var groupsLoader = _groupsLoaderMaker.Make(groupsFilePath, groupsSummary, groups))
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
