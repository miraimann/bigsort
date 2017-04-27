using System;
using System.IO;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation
{
    public class BigSorter
        : IBigSorter
    {
        private readonly IDiagnosticTools _diagnosticTools;
        private readonly IIoService _ioService;
        private readonly IGrouper _grouper;
        private readonly ISorter _sorter;

        public BigSorter(
            IIoService ioService,
            IGrouper grouper,
            ISorter sorter, 
            IDiagnosticTools diagnosticTools = null)
        {
            _ioService = ioService;
            _grouper = grouper;
            _sorter = sorter;
            _diagnosticTools = diagnosticTools;
        }

        public void Sort(string inputPath, string outputPath)
        {
            var fileLength = _ioService.SizeOfFile(inputPath);
            var groupsFile = _ioService.CreateTempFile(fileLength);
            var groupsSummary = _grouper.SplitToGroups(inputPath, groupsFile);

            _ioService.CreateFile(outputPath, fileLength);
            _sorter.Sort(groupsFile, groupsSummary, outputPath);
            _ioService.DeleteFile(groupsFile);

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
