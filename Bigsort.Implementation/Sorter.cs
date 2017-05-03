using System;
using System.Linq;
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

        public Sorter(
            IGroupsLoaderMaker groupsLoaderMaker,
            IGroupSorter groupSorter,
            ISortedGroupWriterFactory sortedGroupWriterFactory)
        {
            _groupsLoaderMaker = groupsLoaderMaker;
            _groupSorter = groupSorter;
            _groupWriterFactory = sortedGroupWriterFactory;
        }

        public void Sort(GroupInfo[] groupsInfo)
        {
            var positions = new long[Consts.MaxGroupsCount];
            var groups = new IGroup[Consts.MaxGroupsCount];
                
            using (var groupsWriter = _groupWriterFactory.Create())
            using (var groupsLoader = _groupsLoaderMaker.Make(groupsInfo, groups))
            {
                var outputPosition = 0L;
                var loadedGroupsRange = groupsLoader.LoadNextGroups();
                while (!Range.IsZero(loadedGroupsRange))
                {
                    int offest = loadedGroupsRange.Offset,
                        over = offest + loadedGroupsRange.Length;
                    
                    Parallel.For(offest, over, Consts.UseMaxTasksCountOptions,
                        i => _groupSorter.Sort(groups[i]));
                    
                    for (int i = offest; i < over; i++)
                        if (groups[i] != null)
                        {
                            positions[i] = outputPosition;
                            outputPosition += groups[i].BytesCount;
                        }

                    Parallel.For(offest, over, Consts.UseMaxTasksCountOptions,
                        i =>
                        {
                            var group = groups[i];
                            if (group != null)
                            {
                                groupsWriter.Write(group, positions[i]);
                                groups[i] = null;
                            }
                        });
                    
                    loadedGroupsRange = groupsLoader.LoadNextGroups();
                }
            }
        }
    }
}
