using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class BigSorter
        : IBigSorter
    {
        private readonly IIoService _ioService;
        private readonly IGrouper _grouper;
        private readonly ISorter _sorter;

        public BigSorter(
            IIoService ioService,
            IGrouper grouper,
            ISorter sorter)
        {
            _ioService = ioService;
            _grouper = grouper;
            _sorter = sorter;
        }

        public void Sort(string inputPath, string outputPath)
        {
            var fileLength = _ioService.SizeOfFile(inputPath);
            var groupsFile = _ioService.CreateTempFile(fileLength);
            var groupsSummary = _grouper.SplitToGroups(inputPath, groupsFile);

            _ioService.CreateFile(outputPath, fileLength);
            _sorter.Sort(groupsFile, groupsSummary, outputPath);
            _ioService.DeleteFile(groupsFile);
        }
    }
}
