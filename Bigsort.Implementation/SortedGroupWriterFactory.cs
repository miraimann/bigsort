﻿using System.Diagnostics;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation
{
    internal class SortedGroupWriterFactory
        : ISortedGroupWriterFactory
    {
        private readonly ITimeTracker _timeTracker;
        private readonly IPoolMaker _poolMaker;
        private readonly IIoService _ioService;
        private readonly IConfig _config;

        public SortedGroupWriterFactory(
            IPoolMaker poolMaker, 
            IIoService ioService, 
            IConfig config,
            IDiagnosticTools diagnosticTools = null)
        {
            _poolMaker = poolMaker;
            _ioService = ioService;
            _config = config;
            _timeTracker = diagnosticTools?.TimeTracker;
        }

        public ISortedGroupWriter Create() =>
            new SortedGroupWriter(
                _poolMaker,
                _ioService,
                _config,
                _timeTracker);

        private class SortedGroupWriter
            : ISortedGroupWriter
        {
            public const string
                LogName = nameof(SortedGroupWriter),
                WriteLogName = LogName + "." + nameof(Write);

            private readonly ITimeTracker _timeTracker;
            private readonly IDisposablePool<IFileWriter> _writersPool;
            private readonly IConfig _config;

            public SortedGroupWriter(
                IPoolMaker poolMaker,
                IIoService ioService,
                IConfig config, 
                ITimeTracker timeTracker)
            {
                _writersPool = poolMaker.MakeDisposablePool(
                    () => ioService.OpenWrite(config.OutputFilePath, buffering: true));

                _config = config;
                _timeTracker = timeTracker;
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
                        int lineLength = line.DigitsCount + line.LettersCount + 3,
                            start = line.Start + 2,
                            i = start/bufferLength,
                            j = start%bufferLength,
                            buffLeftLength = bufferLength - j;

                        bool isLastLineInGroup =
                            line.Start + lineLength == bytesCount;

                        var buff = buffers.Array[buffers.Offset + i];
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
                                    output.Write(buffers.Array[buffers.Offset + i + 1], 0, nextLength);
                                }

                                output.Write(Consts.EndLineBytes, 0, Consts.EndLineBytesCount);
                                continue;
                            }

                            var nextBuff = buffers.Array[buffers.Offset + i + 1];
                            nextBuff[nextLength - 1] = Consts.EndLineByte2;
                            output.Write(buff, j, buffLeftLength);
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
