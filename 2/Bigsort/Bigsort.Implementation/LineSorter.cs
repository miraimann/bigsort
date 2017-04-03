using Bigsort.Contracts;
using System;
using System.IO;
using System.Linq;

namespace Bigsort.Implementation
{
    public class LineSorter<TSegment>
        : ILinesSorter
    {
        private readonly ILinesReservation<TSegment> _linesReservation; 
        private readonly IGrouper _grouper;
        private readonly IGroupBytesLoader _groupLoader;
        private readonly IGroupSorter _groupSorter;
        private readonly ISortedGroupWriter _sortedGroupWriter;
        private readonly IIoService _ioService;
        private readonly ITasksQueue _tasksQueue;
        private readonly IConfig _config;

        public LineSorter(
            ILinesReservation<TSegment> linesReservation,
            IGrouper grouper,
            IGroupBytesLoader groupLoader,
            IGroupSorter groupSorter,
            ISortedGroupWriter sortedGroupWriter,
            IIoService ioService,
            ITasksQueue tasksQueue,
            IConfig config)
        {
            _linesReservation = linesReservation;
            _grouper = grouper;
            _groupLoader = groupLoader;
            _groupSorter = groupSorter;
            _sortedGroupWriter = sortedGroupWriter;
            _ioService = ioService;
            _tasksQueue = tasksQueue;
            _config = config;
        }

        public void Sort(string inputPath, string outputPath)
        {
            var prevCurrentDirectory = _ioService.CurrentDirectory;
            _ioService.CurrentDirectory = _ioService.TempDirectory;
            
            var groupSeeds = _grouper.SplitToGroups(inputPath, 
                _config.PartsDirectory);

            var fileLength = _ioService.SizeOfFile(inputPath);
            _ioService.CreateFile(outputPath, fileLength);
            _linesReservation.Load();

            var o = new object();
            int usedRowsCount = 0;
            long possition = 0;

            _ioService.CurrentDirectory += _config.PartsDirectory;
            foreach (var info in groupSeeds
                .Select(_groupLoader.CalculateMatrixInfo))
            {
                var groupPosition = possition;
                Action sortGroup = null;
                sortGroup = () =>
                {
                    bool reenqueue = false;
                    lock (o)
                        if (usedRowsCount + info.RowsCount >
                            _config.MaxGroupsBuffersCount)
                            reenqueue = true;
                        else usedRowsCount += info.RowsCount;

                    if (!reenqueue)
                    {
                        var rangeHandle = _linesReservation
                            .TryReserveRange(info.BytesCount);

                        if (rangeHandle == null)
                            reenqueue = true;

                        if (!reenqueue)
                            using (rangeHandle)
                            using (var output = _ioService
                                .OpenSharedWrite(outputPath, groupPosition))
                            {
                                var linesRange = rangeHandle.Value;
                                var group = _groupLoader.LoadMatrix(info);

                                _groupSorter.Sort(group, linesRange);
                                _sortedGroupWriter.Write(group, linesRange, output);
                                _ioService.DeleteFile(info.Name);
                            }
                    }

                    if (reenqueue)
                        _tasksQueue.Enqueue(sortGroup);
                };
                
                _tasksQueue.Enqueue(sortGroup);
                possition += info.BytesCount;
            }

            _ioService.CurrentDirectory = prevCurrentDirectory;
        }
    }
}
