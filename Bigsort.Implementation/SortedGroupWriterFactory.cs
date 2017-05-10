using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class SortedGroupWriterFactory
        : ISortedGroupWriterFactory
    {
        private readonly IPoolMaker _poolMaker;
        private readonly IIoService _ioService;
        private readonly IConfig _config;

        public SortedGroupWriterFactory(
            IPoolMaker poolMaker, 
            IIoService ioService, 
            IConfig config)
        {
            _poolMaker = poolMaker;
            _ioService = ioService;
            _config = config;
        }

        public ISortedGroupWriter Create() =>
            new SortedGroupWriter(
                _poolMaker,
                _ioService,
                _config);

        private class SortedGroupWriter
            : ISortedGroupWriter
        {
            private readonly IDisposablePool<IFileWriter> _writersPool;
            private readonly IConfig _config;

            public SortedGroupWriter(
                IPoolMaker poolMaker,
                IIoService ioService,
                IConfig config)
            {
                _writersPool = poolMaker.MakeDisposablePool(
                    () => ioService.OpenWrite(config.OutputFilePath, buffering: true));

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
            }

            public void Dispose() =>
                _writersPool.Dispose();
        }
    }
}
