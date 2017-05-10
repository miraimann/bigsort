using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class Sorter
        : ISorter
    {
        private readonly IIoService _ioService;
        private readonly IGrouper _grouper;
        private readonly IGroupsLoaderMaker _groupsLoaderMaker;
        private readonly IGroupSorter _groupSorter;
        private readonly ISortedGroupWriterFactory _groupWriterFactory;
        private readonly IConfig _config;

        public Sorter(
            IIoService ioService,
            IGrouper grouper,
            IGroupsLoaderMaker groupsLoaderMaker, 
            IGroupSorter groupSorter, 
            ISortedGroupWriterFactory groupWriterFactory, 
            IConfig config)
        {
            _ioService = ioService;
            _grouper = grouper;
            _groupsLoaderMaker = groupsLoaderMaker;
            _groupSorter = groupSorter;
            _groupWriterFactory = groupWriterFactory;
            _config = config;
        }

        public void Sort()
        {
            var fileLength = _ioService.SizeOfFile(_config.InputFilePath);
            _ioService.CreateFile(_config.GroupsFilePath, fileLength);

            var groupsInfo = _grouper.SplitToGroups();
            var positions = new long[Consts.MaxGroupsCount];
            var groups = new IGroup[Consts.MaxGroupsCount];

            _ioService.CreateFile(_config.OutputFilePath, fileLength);

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

            _ioService.DeleteFile(_config.GroupsFilePath);
        }
    }
}
