using System;
using System.IO;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation
{
    public class BigSorter
        : IBigSorter
    {
        private readonly string _inputFilePath, _outputFilePath, _groupsFilePath;
        private readonly IDiagnosticTools _diagnosticTools;
        private readonly IIoService _ioService;
        private readonly IGrouper _grouper;
        private readonly ISorter _sorter;

        public BigSorter(
            string inputFilePath, 
            string outputFilePath,
            string groupsFilePath,
            IIoService ioService,
            IGrouper grouper,
            ISorter sorter,
            IDiagnosticTools diagnosticTools = null)
        {
            _ioService = ioService;
            _grouper = grouper;
            _sorter = sorter;
            _groupsFilePath = groupsFilePath;
            _inputFilePath = inputFilePath;
            _outputFilePath = outputFilePath;
            _diagnosticTools = diagnosticTools;
        }

        public void Sort()
        {
            var fileLength = _ioService.SizeOfFile(_inputFilePath);
            _ioService.CreateFile(_groupsFilePath, fileLength);
            var groupsSummary = _grouper.SplitToGroups();

            _ioService.CreateFile(_outputFilePath, fileLength);
            _sorter.Sort(groupsSummary);
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
