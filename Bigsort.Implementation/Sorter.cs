using System;
using System.IO;
using System.Threading.Tasks;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation
{
    public class Sorter
        : ISorter
    {
        private readonly string _inputFilePath, _outputFilePath, _groupsFilePath;
        private readonly IDiagnosticTools _diagnosticTools;
        private readonly IIoService _ioService;
        private readonly IGrouper _grouper;
        private readonly IGroupsLoaderMaker _groupsLoaderMaker;
        private readonly IGroupSorter _groupSorter;
        private readonly ISortedGroupWriterFactory _groupWriterFactory;

        public Sorter(
            string inputFilePath, 
            string outputFilePath,
            string groupsFilePath,
            IIoService ioService,
            IGrouper grouper,
            IGroupsLoaderMaker groupsLoaderMaker, 
            IGroupSorter groupSorter, 
            ISortedGroupWriterFactory groupWriterFactory, 
            IDiagnosticTools diagnosticTools = null)
        {
            _ioService = ioService;
            _grouper = grouper;
            _groupsLoaderMaker = groupsLoaderMaker;
            _groupSorter = groupSorter;
            _groupWriterFactory = groupWriterFactory;
            _groupsFilePath = groupsFilePath;
            _inputFilePath = inputFilePath;
            _outputFilePath = outputFilePath;
            _diagnosticTools = diagnosticTools;
        }

        public void Sort()
        {
            var fileLength = _ioService.SizeOfFile(_inputFilePath);
            _ioService.CreateFile(_groupsFilePath, fileLength);

            var groupsInfo = _grouper.SplitToGroups();
            var positions = new long[Consts.MaxGroupsCount];
            var groups = new IGroup[Consts.MaxGroupsCount];

            _ioService.CreateFile(_outputFilePath, fileLength);

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

            _ioService.DeleteFile(_groupsFilePath);

            using (var bigSortLogStream = File.OpenWrite("E:\\bslog.txt"))
            using (var logWriter = new StreamWriter(bigSortLogStream))
                foreach (var time in _diagnosticTools.TimeTracker.All)
                {
                    Console.WriteLine($"{time.Value} | {time.Key}");
                    logWriter.WriteLine($"{time.Value} | {time.Key}");
                }
        }
    }
}
