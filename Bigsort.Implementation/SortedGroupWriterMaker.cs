using System;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation
{
    public class SortedGroupWriterMaker
        : ISortedGroupWriterMaker
    {
        private readonly IDiagnosticTools _diagnosticsTool;

        private readonly IIoService _ioService;
        private readonly IPoolMaker _poolMaker;
        private readonly ILinesIndexesStorage _linesIndexesStorage; 

        public SortedGroupWriterMaker(
            IIoService ioService, 
            IPoolMaker poolMaker, 
            ILinesIndexesStorage linesIndexesStorage, 
            IDiagnosticTools diagnosticsTool = null)
        {
            _ioService = ioService;
            _poolMaker = poolMaker;
            _linesIndexesStorage = linesIndexesStorage;
            _diagnosticsTool = diagnosticsTool;
        }

        public ISortedGroupWriter Make(string outputFilepath)
        {
            var writersPool = _poolMaker.Make(
                       productFactory: () => _ioService.OpenWrite(outputFilepath, buffering: true),
                    productDestructor: writer => writer.Dispose());

            return new Writer(_linesIndexesStorage, writersPool, _diagnosticsTool);
        }

        public class Writer
            : ISortedGroupWriter
        {
            public const string
                LogName = nameof(ISortedGroupWriter),
                WriteLogName = LogName + "." + nameof(Write);

            private readonly ITimeTracker _timeTracker;
            
            private readonly IPool<IFileWriter> _writersPool;
            private readonly ILinesIndexesStorage _linesStorage;

            public Writer(
                ILinesIndexesStorage linesStorage, 
                IPool<IFileWriter> writersPool, 
                IDiagnosticTools diagnosticTools)
            {
                _linesStorage = linesStorage;
                _writersPool = writersPool;
                _timeTracker = diagnosticTools?.TimeTracker;
            }

            public void Write(IGroup group, long position)
            {
                var t = DateTime.Now;

                var lines = _linesStorage.Indexes;

                var rows = group.Buffers;
                int rowLength = group.RowLength,
                    bytesCount = group.BytesCount,
                    offset = group.LinesRange.Offset,
                    n = offset + group.LinesRange.Length;

                using (var outputHandle = _writersPool.Get())
                {
                    var output = outputHandle.Value;
                    output.Position = position;

                    while (offset < n)
                    {
                        var line = lines[offset++];
                        int lineLength = line.digitsCount + line.lettersCount + 3,
                            start = line.start + 2,
                            i = start/rowLength,
                            j = start%rowLength,
                            rowLeftLength = rowLength - j;

                        bool isLastLineInGroup =
                            line.start + lineLength == bytesCount;

                        var row = rows[i];
                        if (rowLeftLength < lineLength)
                        {
                            var nextLength = lineLength - rowLeftLength;
                            if (isLastLineInGroup)
                            {
                                nextLength = nextLength - Consts.EndLineBytesCount;
                                if (nextLength <= 0)
                                    output.Write(row, j, rowLeftLength + nextLength);
                                else
                                {
                                    output.Write(row, j, rowLeftLength);
                                    output.Write(rows[i + 1], 0, nextLength);
                                }

                                output.Write(Consts.EndLineBytes, 0, Consts.EndLineBytesCount);
                                continue;
                            }

                            var nextRow = rows[i + 1];
                            output.Write(row, j, rowLeftLength);
                            nextRow[nextLength - 1] = Consts.EndLineByte2;
                            output.Write(nextRow, 0, nextLength);
                            continue;
                        }

                        if (isLastLineInGroup)
                        {
                            output.Write(row, j, lineLength - Consts.EndLineBytesCount);
                            output.Write(Consts.EndLineBytes, 0, Consts.EndLineBytesCount);
                        }
                        else
                        {
                            row[j + lineLength - 1] = Consts.EndLineByte2;
                            output.Write(row, j, lineLength);
                        }
                    }

                    output.Flush();
                }

                _timeTracker?.Add(WriteLogName, DateTime.Now - t);
            }

            public void Dispose() =>
                _writersPool.Dispose();
        }
    }
}
