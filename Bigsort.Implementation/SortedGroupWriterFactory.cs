using System.Collections.Concurrent;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class SortedGroupWriterFactory
        : ISortedGroupWriterFactory
    {
        private readonly IIoService _ioService;
        private readonly IConfig _config;

        public SortedGroupWriterFactory(
            IIoService ioService, 
            IConfig config)
        {
            _ioService = ioService;
            _config = config;
        }

        public ISortedGroupWriter Create() =>
            new SortedGroupWriter(
                _ioService,
                _config);

        private class SortedGroupWriter
            : ISortedGroupWriter
        {
            private volatile ConcurrentBag<IFileWriter> _writers;
            private readonly IIoService _ioService;
            private readonly IConfig _config;

            public SortedGroupWriter(
                IIoService ioService,
                IConfig config)
            {
                _writers = new ConcurrentBag<IFileWriter>();
                _ioService = ioService;
                _config = config;
            }

            public void Write(IGroup group, long position)
            {
                var lines = group.Lines.Array;
                var buffers = group.Buffers;

                int bufferLength = _config.UsingBufferLength,
                    bytesCount = group.BytesCount,
                    offset = group.Lines.Offset,
                    n = offset + group.Lines.Count;

                IFileWriter output;
                if (!_writers.TryTake(out output))
                    output = _ioService.OpenWrite(_config.OutputFilePath, buffering: true);
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
                
                if (_writers == null) output.Dispose();
                else _writers.Add(output);
            }

            public void Dispose()
            {
                if (_writers == null) return;
                var writers = _writers.ToArray();
                _writers = null;

                foreach (var writer in writers)
                    writer.Dispose();
            }
        }
    }
}
