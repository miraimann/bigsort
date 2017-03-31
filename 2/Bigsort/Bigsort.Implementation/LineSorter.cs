using Bigsort.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bigsort.Implementation
{
    public class LineSorter
        : ILinesSorter
    {
        private readonly IGrouper_127_255 _grouper;
        private readonly IGroupLoader _groupLoader;
        private readonly IGroupSorter _groupSorter;
        private readonly ISortedGroupWriter _sortedGroupWriter;
        private readonly IIoService _ioService;
        private readonly ITasksQueue _tasksQueue;
        private readonly IPoolMaker _poolMaker;
        private readonly IConfig _config;

        public LineSorter(
            IGrouper_127_255 grouper,
            IGroupLoader groupLoader,
            IGroupSorter groupSorter,
            ISortedGroupWriter sortedGroupWriter,
            IIoService ioService,
            ITasksQueue tasksQueue,
            IPoolMaker poolMaker,
            IConfig config)
        {
            _grouper = grouper;
            _groupLoader = groupLoader;
            _groupSorter = groupSorter;
            _sortedGroupWriter = sortedGroupWriter;
            _ioService = ioService;
            _tasksQueue = tasksQueue;
            _poolMaker = poolMaker;
            _config = config;
        }

        public void Sort(string inputPath, string outputPath)
        {
            var groupsDirecory = Path.Combine(
                _ioService.TempDirectory,
                _config.PartsDirectory);

            var prevCurrentDirectory = _ioService.CurrentDirectory;
            _ioService.CreateDirectory(groupsDirecory);
            _ioService.CurrentDirectory = groupsDirecory;
            
            var groupSeeds = _grouper.SplitToGroups(inputPath);
            var fileLength = _ioService.SizeOfFile(inputPath);
            _ioService.CreateFile(outputPath, fileLength);

            int maxLinesCount = _config.MainArraySize 
                / Marshal.SizeOf<SortingLine>();

            var linesPool = _poolMaker.MakeFragmentsPool(
                new SortingLine[maxLinesCount]);

            _ioService.CreateFile(outputPath, 
                _ioService.SizeOfFile(inputPath));

            var o = new object();
            int usedRowsCount = 0;
            long possition = 0;

            foreach (var seed in groupSeeds)
            {
                var groupPosition = possition;
                Action sortGroup = null;
                sortGroup = () =>
                {
                    bool reenqueue = false;
                    lock (o)
                        if (usedRowsCount + seed.ContentRowsCount >
                            _config.MaxGroupsBuffersCount)
                            reenqueue = true;
                        else usedRowsCount += seed.ContentRowsCount;

                    if (!reenqueue)
                    {
                        var linesHandle = linesPool.TryGet(seed.BytesCount);
                        if (linesHandle == null)
                            reenqueue = true;

                        if (!reenqueue)
                            using (linesHandle)
                            using (var output = _ioService
                                .OpenSharedWrite(outputPath, groupPosition))
                            {
                                var lines = linesHandle.Value;
                                var group = _groupLoader.Load(seed);

                                _groupSorter.Sort(group, lines);
                                _sortedGroupWriter.Write(group, lines, output);
                                _ioService.DeleteFile(seed.Name);
                            }
                    }

                    if (reenqueue)
                        _tasksQueue.Enqueue(sortGroup);
                };
                
                _tasksQueue.Enqueue(sortGroup);
                possition += seed.BytesCount;
            }

            _ioService.CurrentDirectory = prevCurrentDirectory;
        }
    }
}
