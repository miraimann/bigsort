using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class AsyncWritingGrouper
        : IGrouper
    {
        private readonly string _partFileNameMask;
        private readonly ITasksQueueMaker _tasksQueueMaker;
        private readonly IIoService _ioService;
        private readonly IBuffersPool _buffersPool;
        private readonly IConfig _config;

        public AsyncWritingGrouper(
            ITasksQueueMaker tasksQueueMaker, 
            IBuffersPool buffersPool, 
            IIoService ioService, 
            IConfig config)
        {
            _tasksQueueMaker = tasksQueueMaker;
            _ioService = ioService;
            _buffersPool = buffersPool;
            _config = config;

            var ushortDigitsCount =
                (int)Math.Ceiling(Math.Log10(ushort.MaxValue));
            _partFileNameMask = new string('0', ushortDigitsCount);
        }


        public IEnumerable<IGroupInfo> SplitToGroups(
            string inputFile,
            string outputDirectory = null)
        {
            string prevCurrentDirectory = null;
            if (outputDirectory != null)
            {
                prevCurrentDirectory = _ioService.CurrentDirectory;
                if (!Path.IsPathRooted(outputDirectory))
                    outputDirectory = Path.Combine(
                        prevCurrentDirectory,
                        outputDirectory);

                if (!_ioService.DirectoryExists(outputDirectory))
                    _ioService.CreateDirectory(outputDirectory);
                _ioService.CurrentDirectory = outputDirectory;
            }

            int buffLength = _config.BufferSize,
                maxPartsCount = 96 * 96 + 96 + 1;

            const byte dot = Consts.Dot,
                       endLine = Consts.EndLineByte1,
                       endStream = 0,
                       endBuff = 1;

            var tasksQueue = _tasksQueueMaker.Make(
                Environment.ProcessorCount / 2);

            var currentBuffHandle = _buffersPool.GetBuffer();
            var previousBuffHandle = _buffersPool.GetBuffer();
            
            byte[] currentBuff = currentBuffHandle.Value,
                  previousBuff = previousBuffHandle.Value;

            var groups = new Dictionary<ushort, Group>(maxPartsCount);
            using (var inputStream = _ioService.OpenRead(inputFile))
            {
                const int linePrefixLength = 2;
                int lastBuffIndex = buffLength - 1,
                    lettersCount = 0,
                    digitsCount = 0,
                    i = linePrefixLength,
                    j = linePrefixLength;

                ushort id = 0;
                byte c = default(byte);

                int countForRead = lastBuffIndex - linePrefixLength;
                int count = inputStream.Read(currentBuff, linePrefixLength, countForRead);
                if (count == countForRead)
                    currentBuff[lastBuffIndex] = endBuff;
                else currentBuff[count + 1] = endStream;

                State backState = State.None,
                      state = State.ReadNumber;

                while (true)
                {
                    switch (state)
                    {
                        case State.ReadNumber:

                            while (currentBuff[i] > dot) i++;

                            if (j < buffLength)
                                digitsCount += i - j;

                            if (currentBuff[i] == dot)
                            {
                                if (j > buffLength)
                                    digitsCount += i;

                                j = ++i;
                                state = State.ReadId;
                                break;
                            }

                            // buff[i] == endBuff
                            backState = State.ReadNumber;
                            state = State.LoadNextBuff;
                            break;

                        case State.ReadId:

                            var readFirstLetter = id == 0;
                            c = currentBuff[i];

                            if (c > endLine)
                            {
                                if (readFirstLetter)
                                {
                                    id = (ushort)(c * byte.MaxValue);
                                    state = State.ReadId;
                                }
                                else
                                {
                                    id += c;
                                    state = State.ReadString;
                                }

                                ++i;
                                break;
                            }

                            lettersCount = readFirstLetter ? 0 : 1;
                            if (c == endLine)
                            {
                                state = State.ReleaseLine;
                                break;
                            }

                            // c == endBuff
                            backState = State.ReadId;
                            state = State.LoadNextBuff;
                            break;

                        case State.ReadString:

                            while (currentBuff[i] > endLine) i++;

                            if (j < buffLength)
                                lettersCount += i - j;

                            if (currentBuff[i] == endLine)
                            {
                                if (j > buffLength)
                                    lettersCount += i;

                                state = State.ReleaseLine;
                                break;
                            }

                            // buff[i] == endBuff
                            backState = State.ReadString;
                            state = State.LoadNextBuff;
                            break;

                        case State.LoadNextBuff:

                            j += buffLength;
                            i = 0;

                            var actualBuff = previousBuff;
                            previousBuff = currentBuff;
                            currentBuff = actualBuff;

                            count = inputStream.Read(actualBuff, 0, lastBuffIndex);
                            if (count == lastBuffIndex)
                                actualBuff[lastBuffIndex] = endBuff;
                            else
                            {
                                var endStreamIndex = Math.Max(0, count - 1);
                                if (endStreamIndex == 0)
                                {
                                    state = State.Finish;
                                    break;
                                }

                                actualBuff[endStreamIndex] = endStream;
                            }

                            state = backState;
                            break;

                        case State.ReleaseLine:

                            if (!groups.ContainsKey(id))
                            {
                                var name = id.ToString(_partFileNameMask);
                                var group = new Group(name, 
                                    _ioService.OpenBufferingAsyncWrite(name, tasksQueue));
                                groups.Add(id, group);
                            }

                            ++groups[id].LinesCount;

                            var lineLength = digitsCount + lettersCount + 3;
                            var lineStart = i - lineLength;
                            var writer = groups[id].Bytes;

                            if (lineStart < 0)
                            {
                                lineLength = Math.Abs(lineStart);
                                lineStart += lastBuffIndex;

                                previousBuff[lineStart] = (byte)lettersCount;
                                if (lineLength > 1)
                                    previousBuff[lineStart + 1] = (byte)digitsCount;
                                else currentBuff[0] = (byte)digitsCount;

                                writer.Write(previousBuff, lineStart, lineLength);
                                writer.Write(currentBuff, 0, i);
                            }
                            else
                            {
                                currentBuff[lineStart] = (byte)lettersCount;
                                currentBuff[lineStart + 1] = (byte)digitsCount;
                                writer.Write(currentBuff, lineStart, lineLength);
                            }

                            lettersCount = 0;
                            digitsCount = 0;
                            id = 0;

                            if (currentBuff[++i] == endBuff)
                            {
                                backState = State.CheckFinish;
                                state = State.LoadNextBuff;
                                break;
                            }

                            state = State.CheckFinish;
                            break;

                        case State.CheckFinish:
                            state = currentBuff[i++] == endStream
                                  ? State.Finish
                                  : State.ReadNumber;
                            j = i;
                            break;

                        case State.Finish:
                            
                            foreach (var group in groups.Values)
                                group.Bytes.Flush();
                            
                            while (tasksQueue.IsProcessing)
                                Thread.Sleep(100);

                            var option = new ParallelOptions
                            {
                                MaxDegreeOfParallelism = Environment.ProcessorCount
                            };

                            Parallel.ForEach(groups.Values, option,
                                group => group.Bytes.Dispose());

                            if (prevCurrentDirectory != null)
                                _ioService.CurrentDirectory = prevCurrentDirectory;

                            return groups.Values.OrderBy(o => o.Name);
                    }
                }
            }
        }

        private class Group
            : IGroupInfo
        {
            public Group(string name, IWriter writer)
            {
                Name = name;
                Bytes = writer;
            }

            public string Name { get; }
            public IWriter Bytes { get; set; }
            public int LinesCount { get; set; }
            public int BytesCount { get; set; }
        }

        private enum State
        {
            ReadNumber,
            ReadId,
            ReadString,
            ReleaseLine,
            LoadNextBuff,
            CheckFinish,
            Finish,
            None
        }
    }
}
