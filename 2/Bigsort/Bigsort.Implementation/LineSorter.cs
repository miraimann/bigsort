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
        private readonly IIoService _ioService;
        private readonly IConfig _config;

        public LineSorter(
            IGrouper_127_255 grouper, 
            IIoService ioService, 
            IConfig config)
        {
            _grouper = grouper;
            _ioService = ioService;
            _config = config;
        }

        public void Sort(string inputPath, string outputPath)
        {
            var groupsDirectory = _grouper
                .SplitToGroups(inputPath);

            var linesLength = _config.MainArraySize 
                            / Marshal.SizeOf<SortingLine>();

            var lines = new SortingLine[linesLength];

            foreach (var file in _ioService
                .EnumerateFilesOf(groupsDirectory))
            {
                   
            }
        }
    }
}
