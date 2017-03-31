using Bigsort.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Bigsort.Implementation
{
    public class LineSorter
        : ILinesSorter
    {
        private readonly IGrouper_127_255 _grouper;
        private readonly IGroupService _groupService;
        private readonly IIoService _ioService;
        private readonly ITasksQueue _tasksQueue;
        private readonly IConfig _config;

        public LineSorter(
            IGrouper_127_255 grouper,
            IGroupService groupService,
            IIoService ioService,
            ITasksQueue tasksQueue,
            IConfig config)
        {
            _grouper = grouper;
            _groupService = groupService;
            _ioService = ioService;
            _tasksQueue = tasksQueue;
            _config = config;
        }

        public void Sort(string inputPath, string outputPath)
        {
            var groupsDirectory = _grouper
                .SplitToGroups(inputPath);

            int maxLinesCount =
                    _config.MainArraySize/Marshal.SizeOf<SortingLine>(),
                freeLinesCount = maxLinesCount;

            var lines = new SortingLine[maxLinesCount];

            foreach (var file in _ioService
                .EnumerateFilesOf(groupsDirectory))
            {
                _groupService.LinesCountOfGroup(file);
            }
        }
    }
}
