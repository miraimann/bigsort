using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class Grouper
        : IGrouper
    {
        private readonly string _partFileNameMask;
        private readonly IIoService _ioService;
        private readonly IConfig _config;

        public Grouper(IIoService ioService, IConfig config)
        {
            _ioService = ioService;
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

            const int current = 0, 
                     previous = 1;
            
            byte[][] buffs = new byte[2][];
            buffs[current] = new byte[buffLength];
            buffs[previous] = new byte[buffLength];
            
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
                byte c;

                int countForRead = lastBuffIndex - linePrefixLength;
                int count = inputStream.Read(buffs[current], linePrefixLength, countForRead);
                if (count == countForRead)
                    buffs[current][lastBuffIndex] = endBuff;
                else buffs[current][count + 1] = endStream;

                State backState = State.None,
                      state = State.ReadNumber;

                while (true)
                {
                    var currentBuff = buffs[current];
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

                            var actualBuff = buffs[previous];
                            buffs[previous] = buffs[current];
                            buffs[current] = actualBuff;
                            
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
                                var group = new Group(name, _ioService.OpenWrite(name));
                                groups.Add(id, group);
                            }

                            ++groups[id].LinesCount;

                            var lineLength = digitsCount + lettersCount + 3;
                            var lineStart = i - lineLength;
                            var writer = groups[id].Bytes;

                            if (lineStart < 0)
                            {
                                var prevBuff = buffs[previous];
                                lineLength = Math.Abs(lineStart);
                                lineStart += lastBuffIndex;
                                
                                prevBuff[lineStart] = (byte) lettersCount;
                                if (lineLength > 1)
                                    prevBuff[lineStart + 1] = (byte) digitsCount;
                                else currentBuff[0] = (byte) digitsCount;
                                
                                writer.Write(prevBuff, lineStart, lineLength);
                                writer.Write(currentBuff, 0, i);
                            }
                            else
                            {
                                currentBuff[lineStart] = (byte) lettersCount;
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

                            var option = new ParallelOptions
                            {
                                MaxDegreeOfParallelism = Environment.ProcessorCount
                            };

                            Parallel.ForEach(groups.Values, option,
                                group =>
                                {
                                    group.Bytes.Flush();
                                    group.BytesCount = (int)group.Bytes.Length;
                                    group.Bytes.Dispose();
                                    group.Bytes = null;
                                });

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
