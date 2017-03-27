using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class Grouper
        : IGrouper
    {
        private readonly string
            _partFileNameMask,
            _partsDirecory;

        private readonly IIoService _ioService;
        private readonly IConfig _config;

        public Grouper(IIoService ioService, IConfig config)
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
            int buffLength = _config.BufferSize * 3,
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

            var parts = new Dictionary<ushort, IWriter>(maxPartsCount);
            var prevCurrentDirectory = _ioService.CurrentDirectory;
            _ioService.CurrentDirectory = _partsDirecory;

            using (var inputStream = _ioService.OpenRead(filePath))
            {
                const int linePrefixLength = 2;
                int lastBuffIndex = buffLength - 1,
                    lettersCountByte1 = 0,
                    lettersCountByte2 = 0,
                    lettersCount = 0,
                    digitsCountByte = 1,
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

                            if (digitsCountByte < buffLength)
                                digitsCount += (i - digitsCountByte - 1);

                            if (currentBuff[i] == dot)
                            {
                                if (digitsCountByte > buffLength)
                                    digitsCount += i;

                                buffs[digitsCountByte / buffLength]
                                     [digitsCountByte % buffLength] = (byte)digitsCount;

                                lettersCountByte2 = i++;
                                state = State.ReadStringFirstByte;
                                break;
                            }

                            // buff[i] == endBuff
                            backState = State.ReadNumber;
                            state = State.LoadNextBuff;
                            break;

                        case State.ReadStringFirstByte:

                            c = currentBuff[i];
                            if (c > endLine)
                            {
                                id = (ushort)(c * byte.MaxValue);
                                state = State.ReadStringSecondByte;
                                ++i;
                                break;
                            }

                            if (c == endLine)
                            {
                                buffs[lettersCountByte1 / buffLength]
                                     [lettersCountByte1 % buffLength] = 0;
                                buffs[lettersCountByte2 / buffLength]
                                     [lettersCountByte2 % buffLength] = 0;
                                
                                state = State.ReleaseLine;
                                break;
                            }

                            // c == endBuff
                            backState = State.ReadStringFirstByte;
                            state = State.LoadNextBuff;
                            break;

                        case State.ReadStringSecondByte:

                            c = currentBuff[i];
                            if (c > endLine)
                            {
                                id += c;
                                state = State.ReadStringTail;
                                ++i;
                                break;
                            }
                            
                            lettersCount = 1;
                            if (c == endLine)
                            {
                                buffs[lettersCountByte1 / buffLength]
                                     [lettersCountByte1 % buffLength] = 0;
                                buffs[lettersCountByte2 / buffLength]
                                     [lettersCountByte2 % buffLength] = 1;

                                state = State.ReleaseLine;
                                break;
                            }

                            // c == endBuff
                            backState = State.ReadStringSecondByte;
                            state = State.LoadNextBuff;
                            break;

                        case State.ReadStringTail:

                            while (currentBuff[i] > endLine) i++;

                            if (lettersCountByte2 < buffLength)
                                lettersCount += i - lettersCountByte2 - 1;

                            if (currentBuff[i] == endLine)
                            {
                                if (lettersCountByte2 > buffLength)
                                    lettersCount += i;

                                buffs[lettersCountByte1 / buffLength]
                                     [lettersCountByte1 % buffLength] = 
                                            (byte)(lettersCount / byte.MaxValue);

                                buffs[lettersCountByte2 / buffLength]
                                     [lettersCountByte2 % buffLength] = 
                                            (byte)(lettersCount % byte.MaxValue);

                                state = State.ReleaseLine;
                                break;
                            }

                            // buff[i] == endBuff
                            backState = State.ReadStringTail;
                            state = State.LoadNextBuff;
                            break;
                            
                        case State.LoadNextBuff:

                            switch (backState)
                            {
                                case State.ReadStringFirstByte:
                                case State.ReadStringSecondByte:
                                case State.ReadStringTail:
                                    lettersCountByte2 += buffLength;
                                    goto case State.ReadNumber;

                                case State.ReadNumber:
                                    lettersCountByte1 += buffLength;
                                    digitsCountByte += buffLength;
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

                            var lineLength = digitsCount + lettersCount + 3;
                            var lineStart = i - lineLength;
                            var writer = parts[id];

                            if (lineStart < 0)
                            {
                                lineStart = Math.Abs(lineStart);
                                writer.Write(buffs[previous],
                                             lastBuffIndex - lineStart,
                                             lineStart);

                                writer.Write(currentBuff, 0, i);
                            }
                            else
                                writer.Write(currentBuff,
                                             lettersCountByte1,
                                             lineLength);
                            
                            lettersCount = 0;
                            digitsCount = 0;
                            id = 0;

                            if (currentBuff[++i] == endBuff)
                            {
                                lettersCountByte1 = i + buffLength - 1;
                                digitsCountByte = 0;
                                i = 0;

                                backState = State.CheckFinish;
                                state = State.LoadNextBuff;
                                break;
                            }

                            lettersCountByte1 = i - 1;
                            digitsCountByte = i;
                            state = State.CheckFinish;
                            break;

                        case State.CheckFinish:
                            state = currentBuff[i++] == endStream
                                  ? State.Finish
                                  : State.ReadNumber;
                            break;

                        case State.Finish:
                            foreach (var p in parts.Values)
                                p.Dispose();

                            _ioService.CurrentDirectory = prevCurrentDirectory;
                            return _partsDirecory;
                    }
                }
            }
        }

        private enum State
        {
            ReadNumber,
            ReadStringFirstByte,
            ReadStringSecondByte,
            ReadStringTail,
            ReleaseLine,
            LoadNextBuff,
            CheckFinish,
            Finish,
            None
        }
    }
}
