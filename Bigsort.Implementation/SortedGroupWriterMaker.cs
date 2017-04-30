using System;
using System.Diagnostics;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation
{
    public class SortedGroupWriterMaker
        : ISortedGroupWriterMaker
    {
        private readonly IDiagnosticTools _diagnosticsTool;

        private readonly IIoServiceMaker _ioServiceMaker;
        private readonly IPoolMaker _poolMaker;
        private readonly IConfig _config;

        public SortedGroupWriterMaker(
            IIoServiceMaker ioServiceMaker, 
            IPoolMaker poolMaker, 
            IConfig config,
            IDiagnosticTools diagnosticsTool = null)
        {
            _ioServiceMaker = ioServiceMaker;
            _poolMaker = poolMaker;
            _config = config;
            _diagnosticsTool = diagnosticsTool;
        }

        public ISortedGroupWriter Make(
            string outputFilepath,
            IPool<byte[]> buffersPool)
        {
            var ioService = _ioServiceMaker.Make(buffersPool);
            var writersPool = _poolMaker.MakeDisposablePool(
                         () => ioService.OpenWrite(outputFilepath, buffering: true));

            return new Writer( 
                writersPool,
                _config,
                _diagnosticsTool);
        }

        public class Writer
            : ISortedGroupWriter
        {
            public const string
                LogName = nameof(ISortedGroupWriter),
                WriteLogName = LogName + "." + nameof(Write);

            private readonly ITimeTracker _timeTracker;
            
            private readonly IDisposablePool<IFileWriter> _writersPool;
            private readonly IConfig _config;
            
            public Writer(
                IDisposablePool<IFileWriter> writersPool,
                IConfig config, 
                IDiagnosticTools diagnosticTools)
            {
                _writersPool = writersPool;
                _config = config;
                _timeTracker = diagnosticTools?.TimeTracker;
            }

            public void Write(IGroup group, long position)
            {
                var watch = Stopwatch.StartNew();

                var lines = group.Lines.Array;

                var buffers = group.Buffers;
                int bufferLength = _config.UsingBufferLength,
                    bytesCount = group.BytesCount,
                    offset = group.Lines.Offset,
                    n = offset + group.Lines.Count;

                using (var outputHandle = _writersPool.Get())
                {
                    var output = outputHandle.Value;
                    output.Position = position;

                    while (offset < n)
                    {
                        var line = lines[offset++];
                        int lineLength = line.digitsCount + line.lettersCount + 3,
                            start = line.start + 2,
                            i = start / bufferLength,
                            j = start % bufferLength,
                            buffLeftLength = bufferLength - j;

                        bool isLastLineInGroup =
                            line.start + lineLength == bytesCount;

                        var buff = buffers[i];
                        if (buffLeftLength < lineLength)
                        {
                            var nextLength = lineLength - buffLeftLength;
                            if (isLastLineInGroup)
                            {
                                nextLength = nextLength - Consts.EndLineBytesCount;
                                if (nextLength <= 0)
                                    output.Write(buff, j, buffLeftLength + nextLength);
                                else
                                {
                                    output.Write(buff, j, buffLeftLength);
                                    output.Write(buffers[i + 1], 0, nextLength);
                                }

                                output.Write(Consts.EndLineBytes, 0, Consts.EndLineBytesCount);
                                continue;
                            }

                            var nextBuff = buffers[i + 1];
                            output.Write(buff, j, buffLeftLength);
                            nextBuff[nextLength - 1] = Consts.EndLineByte2;
                            output.Write(nextBuff, 0, nextLength);
                            continue;
                        }

                        if (isLastLineInGroup)
                        {
                            output.Write(buff, j, lineLength - Consts.EndLineBytesCount);
                            output.Write(Consts.EndLineBytes, 0, Consts.EndLineBytesCount);
                        }
                        else
                        {
                            buff[j + lineLength - 1] = Consts.EndLineByte2;
                            output.Write(buff, j, lineLength);
                        }
                    }

                    output.Flush();
                }

                _timeTracker?.Add(WriteLogName, watch.Elapsed);
            }

            public void Dispose() =>
                _writersPool.Dispose();
        }
    }
}
