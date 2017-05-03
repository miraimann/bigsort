using System.Threading;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class Sorter
        : ISorter
    {
        private readonly IGroupsLoaderMaker _groupsLoaderMaker;
        private readonly IGroupSorter _groupSorter;
        private readonly ISortedGroupWriterFactory _groupWriterFactory;
        private readonly ITasksQueue _tasksQueue;

        public Sorter(
            IGroupsLoaderMaker groupsLoaderMaker,
            IGroupSorter groupSorter,
            ISortedGroupWriterFactory sortedGroupWriterFactory,
            ITasksQueue tasksQueue)
        {
            _groupsLoaderMaker = groupsLoaderMaker;
            _groupSorter = groupSorter;
            _groupWriterFactory = sortedGroupWriterFactory;
            _tasksQueue = tasksQueue;
        }

        public void Sort(GroupInfo[] groupsInfo)
        {
            var groups = new IGroup[Consts.MaxGroupsCount];
                
            using (var groupsWriter = _groupWriterFactory.Create())
            using (var groupsLoader = _groupsLoaderMaker.Make(groupsInfo, groups))
            {
                var outputPosition = 0L;
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

                    i = loadedGroupsRange.Offset;
                    while (i != n)
                    {
                        int j = i++;
                        var group = groups[j];
                        if (group == null)
                        {
                            groupsBlockDone.Signal();
                            continue;
                        }
                        
                        long p = outputPosition;
                        outputPosition += group.BytesCount;
                        _tasksQueue.Enqueue(delegate
                        {
                            groupsWriter.Write(group, p);
                            groups[j] = null;
                            groupsBlockDone.Signal();
                        });
                    }

                    groupsBlockDone.Wait();
                    loadedGroupsRange = groupsLoader.LoadNextGroups();
                }
            }
        }
    }
}
