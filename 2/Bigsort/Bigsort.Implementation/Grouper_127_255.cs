using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    // ReSharper disable once InconsistentNaming
    public class Grouper_127_255
        : IGrouper_127_255
    {
        private readonly string
            _partFileNameMask,
            _partsDirecory;

        private readonly IIoService _ioService;
        private readonly IConfig _config;

        public Grouper_127_255(IIoService ioService, IConfig config)
        {
            _ioService = ioService;
            _config = config;

            var ushortDigitsCount =
                (int)Math.Ceiling(Math.Log10(ushort.MaxValue));
            _partFileNameMask = new string('0', ushortDigitsCount);

            _partsDirecory = Path.Combine(
                _ioService.TempDirectory,
                _config.PartsDirectory);
        }

        public string SplitToGroups(string filePath)
        {
            int buffLength = _config.BufferSize,
                maxPartsCount = 96 * 96 + 96 + 1;

            const byte dot = (byte)'.',
                       endLine = (byte)'\r',
                       endStream = 0,
                       endBuff = 1;

            const int current = 0, 
                     previous = 1;
            
            byte[][] buffs = new byte[2][];
            buffs[current] = new byte[buffLength];
            buffs[previous] = new byte[buffLength];

            var prevCurrentDirectory = _ioService.CurrentDirectory;
            _ioService.CreateDirectory(_partsDirecory);
            _ioService.CurrentDirectory = _partsDirecory;

            var parts = new Dictionary<ushort, IWriter>(maxPartsCount);
            using (var inputStream = _ioService.OpenRead(filePath))
            {
                const int linePrefixLength = 2;
                int lastBuffIndex = buffLength - 1,
                    lettersIndex = 0,
                    digitsIndex = 1,
                    lettersCount = 0,
                    digitsCount = 0,
                    i = 2;

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

                            if (digitsIndex < buffLength)
                                digitsCount += i - digitsIndex - 1;

                            if (currentBuff[i] == dot)
                            {
                                if (digitsIndex > buffLength)
                                    digitsCount += i;
                                
                                lettersIndex = i++;
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
                            backState = readFirstLetter
                                ? State.ReadId
                                : State.ReadString;
                            state = State.LoadNextBuff;
                            break;

                        case State.ReadString:

                            while (currentBuff[i] > endLine) i++;

                            if (lettersIndex < buffLength)
                                lettersCount += i - lettersIndex - 1;

                            if (currentBuff[i] == endLine)
                            {
                                if (lettersIndex > buffLength)
                                    lettersCount += i;

                                state = State.ReleaseLine;
                                break;
                            }

                            // buff[i] == endBuff
                            backState = State.ReadString;
                            state = State.LoadNextBuff;
                            break;
                            
                        case State.LoadNextBuff:

                            switch (backState)
                            {
                                case State.ReadId:
                                case State.ReadString:
                                    lettersIndex += buffLength;
                                    goto case State.ReadNumber;

                                case State.ReadNumber:
                                    digitsIndex += buffLength;
                                    i = 0;
                                    break;
                            }
                            
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

                            if (!parts.ContainsKey(id))
                            {
                                var name = id.ToString(_partFileNameMask);
                                parts.Add(id, _ioService.OpenWrite(name));
                            }

                            var prevBuff = buffs[previous];

                            if (lettersIndex < buffLength)
                            {
                                currentBuff[lettersIndex] = (byte) lettersCount;
                                if (digitsIndex < buffLength)
                                     currentBuff[digitsIndex] = (byte) digitsCount;
                                else
                                    prevBuff[digitsIndex - buffLength] = 
                                        (byte) digitsCount;
                            }
                            else
                            {
                                prevBuff[digitsIndex - buffLength] = 
                                    (byte) digitsCount;
                                prevBuff[lettersIndex - buffLength] = 
                                    (byte) lettersCount;
                            }

                            var lineLength = digitsCount + lettersCount + 3;
                            var lineStart = i - lineLength;
                            var writer = parts[id];

                            if (lineStart < 0)
                            {
                                lineLength = Math.Abs(lineStart);
                                lineStart += lastBuffIndex;   
                                prevBuff[lineStart] = linePrefixLength;
                                
                                writer.Write(prevBuff, lineStart, lineLength);
                                writer.Write(currentBuff, 0, i);
                            }
                            else
                            {
                                currentBuff[lineStart] = linePrefixLength;
                                writer.Write(currentBuff, lineStart, lineLength);
                            }

                            lettersCount = 0;
                            digitsCount = 0;
                            id = 0;

                            if (currentBuff[++i] == endBuff)
                            {
                                digitsIndex = i = 0;
                                backState = State.CheckFinish;
                                state = State.LoadNextBuff;
                                break;
                            }
                            
                            digitsIndex = i;
                            state = State.CheckFinish;
                            break;

                        case State.CheckFinish:
                            state = currentBuff[i++] == endStream
                                  ? State.Finish
                                  : State.ReadNumber;
                            break;

                        case State.Finish:

                            var option = new ParallelOptions
                            {
                                MaxDegreeOfParallelism = Environment.ProcessorCount
                            };

                            Parallel.ForEach(parts.Values, option, p => p.Dispose());
                            _ioService.CurrentDirectory = prevCurrentDirectory;
                            return _partsDirecory;
                    }
                }
            }
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
