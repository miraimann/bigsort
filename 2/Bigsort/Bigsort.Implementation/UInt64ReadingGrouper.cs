using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class UInt64ReadingGrouper
            : IGrouper
    {
        private readonly string _partFileNameMask;
        private readonly IBytesEnumeratorMaker _bytesEnumeratorMaker;
        private readonly IIoService _ioService;
        private readonly IConfig _config;

        public UInt64ReadingGrouper(
            IBytesEnumeratorMaker bytesEnumeratorMaker,
            IIoService ioService, 
            IConfig config)
        {
            _bytesEnumeratorMaker = bytesEnumeratorMaker;
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
            
            byte[] currentBuff = new byte[buffLength],
                  previousBuff = new byte[buffLength];

            var groups = new Dictionary<ushort, Group>(maxPartsCount);
            using (var inputStream = _ioService.OpenRead(inputFile))
            {
                const int linePrefixLength = 2;
                int lastBuffIndex = buffLength - 1,
                    lettersCount = 0,
                    digitsCount = 0,
                    // i = linePrefixLength,
                    j = linePrefixLength;
                
                ushort id = 0;
                byte c = default(byte);

                int countForRead = lastBuffIndex - linePrefixLength;
                int count = inputStream.Read(currentBuff, linePrefixLength, countForRead);
                if (count == countForRead)
                    currentBuff[lastBuffIndex] = endBuff;
                else currentBuff[count + 1] = endStream;

                var x = new LittleEndianBytesIterator(currentBuff);
                ++x; // skip first byte
                ++x; // and second

                State backState = State.None,
                      state = State.ReadNumber;


                int buffsCount = 0;
                int processedSize = 0;

                while (true)
                {
                    switch (state)
                    {
                        case State.ReadNumber:

                         // while (currentBuff[i] > dot) i++;
                            while (x.value > dot) x++;

                         // if (j < buffLength)
                         //     digitsCount += i - j;
                            if (j < buffLength)
                                digitsCount += x.index - j;

                         // if (currentBuff[i] == dot)
                            if (x.value == dot)
                            {
                                if (j > buffLength)
                                 // digitsCount += i;
                                    digitsCount += x.index;

                             // j = ++i;
                                j = (++x).index;
                                state = State.ReadId;
                                break;
                            }

                            // buff[i] == endBuff
                            backState = State.ReadNumber;
                            state = State.LoadNextBuff;
                            break;

                        case State.ReadId:

                            var readFirstLetter = id == 0;
                         // c = currentBuff[i];
                            c = x.value;

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

                              //++i;
                                ++x;
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

                         // while (currentBuff[i] > endLine) i++;
                            while (x.value > endLine) x++;

                            if (j < buffLength)
                             // lettersCount += i - j;
                                lettersCount += x.index - j;

                         // if (currentBuff[i] == endLine)
                            if (x.value == endLine)
                            {
                                if (j > buffLength)
                                    lettersCount += x.index;

                                state = State.ReleaseLine;
                                break;
                            }

                            // buff[i] == endBuff
                            backState = State.ReadString;
                            state = State.LoadNextBuff;
                            break;

                        case State.LoadNextBuff:

                            buffsCount++;
                            processedSize += buffLength;

                            j += buffLength;
                         // i = 0;

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

                            x = new LittleEndianBytesIterator(actualBuff);
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
                         // var lineStart = i - lineLength;
                            var lineStart = x.index - lineLength;
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
                             // writer.Write(currentBuff, 0, i);
                                writer.Write(currentBuff, 0, x.index);
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

                         // if (currentBuff[++i] == endBuff)
                            if ((++x).value == endBuff)
                            {
                                backState = State.CheckFinish;
                                state = State.LoadNextBuff;
                                break;
                            }

                            state = State.CheckFinish;
                            break;

                        case State.CheckFinish:
                         // state = currentBuff[i++] == endStream
                            state = x++.value == endStream
                                  ? State.Finish
                                  : State.ReadNumber;
                         // j = i;
                            j = x.index;
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

        [StructLayout(LayoutKind.Explicit, 
            Size = sizeof(ulong))]
        private struct UInt64Segment
        {
            [FieldOffset(0)] public ulong value;
            [FieldOffset(0)] public readonly byte byte0;
            [FieldOffset(1)] public readonly byte byte1;
            [FieldOffset(2)] public readonly byte byte2;
            [FieldOffset(3)] public readonly byte byte3;
            [FieldOffset(4)] public readonly byte byte4;
            [FieldOffset(5)] public readonly byte byte5;
            [FieldOffset(6)] public readonly byte byte6;
            [FieldOffset(7)] public readonly byte byte7;
        }
        
        private struct LittleEndianBytesIterator
        {
            public LittleEndianBytesIterator(byte[] source)
                : this(source, 
                       new UInt64Segment{ value = BitConverter.ToUInt64(source, 0) },
                       source[0],
                       0,
                       0)
            {
            }

            private LittleEndianBytesIterator(
                byte[] source, 
                UInt64Segment segment, 
                byte value, 
                int index,
                int segmentByteIndex)
            {
                _source = source;
                _segment = segment;
                _segmentIndex = segmentByteIndex;
                this.value = value;
                this.index = index;
            }
        
            private readonly byte[] _source;
            private readonly UInt64Segment _segment;
            private readonly int _segmentIndex;

            public readonly byte value;
            public readonly int index;
            
            public static LittleEndianBytesIterator operator
                ++(LittleEndianBytesIterator x)
            {
                var i = x.index + 1;
                var j = x._segmentIndex + 1;
                var segment = x._segment;
                
                if (j == sizeof(ulong))
                {
                    j = 0;
                    segment = new UInt64Segment
                    {
                        value = BitConverter.ToUInt64(x._source, i)
                    };
                }

                byte value;
                switch (j)
                {
                    case 0: value = x._segment.byte0; break;
                    case 1: value = x._segment.byte1; break;
                    case 2: value = x._segment.byte2; break;
                    case 3: value = x._segment.byte3; break;
                    case 4: value = x._segment.byte4; break;
                    case 5: value = x._segment.byte5; break;
                    case 6: value = x._segment.byte6; break;
                    case 7: value = x._segment.byte7; break;
                    default: value = 0; break;
                }
                
                return new LittleEndianBytesIterator(
                    x._source, segment, value, i, j);
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
