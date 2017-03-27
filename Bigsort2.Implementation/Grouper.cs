using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bigsort2.Contracts;

namespace Bigsort2.Implementation
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
            
            byte[] prevBuff = new byte[buffLength],
                   buff = new byte[buffLength],
                   lettersCountBuff;

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
                    i = digitsCountByte;

                ushort id = 0;
                byte c;

                int countForRead = lastBuffIndex - linePrefixLength;
                int count = inputStream.Read(buff, linePrefixLength, countForRead);
                if (count == countForRead)
                    buff[lastBuffIndex] = endBuff;
                else buff[count + 1] = endStream;

                State backState = State.LoadNextBuff,
                      state = State.ReadNumber;

                while (true)
                {
                    switch (state)
                    {
                        case State.ReadNumber:
                            
                            while (buff[++i] > dot) ;
                            digitsCount = i - digitsCountByte - 1;
                            if (buff[i] == dot)
                            {
                                buff[digitsCountByte] = (byte)digitsCount;
                                lettersCountByte2 = i;
                                state = State.ReadStringFirstByte;
                                break;
                            }

                            // buff[i] == endBuff
                            backState = State.ContinueNumberReading;
                            state = State.LoadNextBuff;
                            i = 0;
                            break;

                        case State.ReadStringFirstByte:

                            c = buff[++i];
                            if (c > endLine)
                            {
                                id = (ushort)(c * byte.MaxValue);
                                state = State.ReadStringSecondByte;
                                break;
                            }

                            if (c == endLine)
                            {
                                lettersCount = 0;
                                buff[lettersCountByte1] = 0;
                                buff[lettersCountByte2] = 0;
                                state = State.ReleaseLine;
                                break;
                            }

                            // c == endBuff
                            backState = State.ContinueStringFirstByteReading;
                            state = State.LoadNextBuff;

                            break;

                        case State.ReadStringSecondByte:

                            c = buff[++i];
                            if (c > endLine)
                            {
                                id += c;
                                state = State.ReadStringTail;
                                break;
                            }

                            if (c == endLine)
                            {
                                lettersCount = 1;
                                buff[lettersCountByte1] = 0;
                                buff[lettersCountByte2] = 1;
                                state = State.ReleaseLine;
                                break;
                            }

                            // c == endBuff
                            backState = State.ContinueStringSecondByteReading;
                            state = State.LoadNextBuff;

                            break;

                        case State.ReadStringTail:

                            while (buff[++i] > endLine) ;
                            if (buff[i] == endLine)
                            {
                                lettersCount = i - lettersCountByte2 - 1;
                                buff[lettersCountByte1] = (byte)(lettersCount / byte.MaxValue);
                                buff[lettersCountByte2] = (byte)(lettersCount % byte.MaxValue);
                                state = State.ReleaseLine;
                                break;
                            }

                            // buff[i] == endBuff
                            backState = State.ContinueStringTailReading;
                            state = State.LoadNextBuff;
                            break;

                        case State.ContinueNumberReading:

                            while (buff[i] != dot) i++;
                            digitsCount += i - 1;
                            prevBuff[digitsCountByte] = (byte)digitsCount;
                            lettersCountByte2 = i;
                            state = State.ReadStringFirstByte;

                            break;

                        case State.ContinueStringFirstByteReading:

                            c = buff[i++];
                            if (c == endLine)
                            {
                                lettersCount = 0;
                                prevBuff[lettersCountByte1] = 0;
                                buff[lettersCountByte2] = 0;
                                state = State.ReleaseLine;
                                break;
                            }

                            id = (ushort)(c * byte.MaxValue);
                            state = State.ContinueStringSecondByteReading;
                            break;

                        case State.ContinueStringSecondByteReading:

                            c = buff[i++];
                            if (c == endLine)
                            {
                                lettersCount = 1;
                                buff[lettersCountByte1] = 0;
                                buff[lettersCountByte2] = 1;
                                state = State.ReleaseLine;
                                break;
                            }

                            id += c;
                            state = State.ContinueStringTailReading;
                            break;

                        case State.ContinueStringTailReading:

                            while (buff[++i] > endLine) ;

                            lettersCount = i - lettersCountByte2 - 1;
                            buff[lettersCountByte1] = (byte)(lettersCount / byte.MaxValue);
                            buff[lettersCountByte2] = (byte)(lettersCount % byte.MaxValue);
                            state = State.ReleaseLine;
                            break;

                        case State.LoadNextBuff:
                            
                            var tmp = buff;
                            buff = prevBuff;
                            prevBuff = tmp;

                            count = inputStream.Read(buff, 0, lastBuffIndex);
                            if (count == lastBuffIndex)
                                buff[lastBuffIndex] = endBuff;
                            else buff[Math.Max(0, count - 1)] = endStream;

                            state = backState;

                            break;

                        case State.ReleaseLine:
                            
                            if (!parts.ContainsKey(id))
                            {
                                var name = id.ToString(_partFileNameMask);
                                parts.Add(id, _ioService.OpenWrite(name));
                            }

                            parts[id].Write(buff, lettersCountByte1,
                                i - lettersCountByte1);

                            if (buff[++i] == endBuff)
                            {
                                lettersCountByte1 = 0;
                                digitsCountByte = 1;
                                i = digitsCountByte;

                                backState = State.CheckFinish;
                                state = State.LoadNextBuff;
                                break;
                            }

                            lettersCountByte1 = i - 1;
                            digitsCountByte = i;
                            state = State.CheckFinish;
                            break;

                        case State.ReleaseDoubleBufferedLine:
                            
                            if (!parts.ContainsKey(id))
                            {
                                var name = id.ToString(_partFileNameMask);
                                parts.Add(id, _ioService.OpenWrite(name));
                            }

                            var lineLength = digitsCount + lettersCount + 3;
                            var prevBuffLineLength = lineLength - 1;

                            var currentWriter = parts[id];
                            currentWriter.Write(prevBuff,
                                lastBuffIndex - prevBuffLineLength,
                                prevBuffLineLength);

                            currentWriter.Write(buff, 0, ++i + 1);

                            if (buff[i] == endBuff)
                            {
                                backState = State.CheckFinish;
                                state = State.LoadNextBuff;
                                break;
                            }

                            state = State.ReadNumber;
                            break;

                        case State.CheckFinish:
                            state = buff[i] == endStream
                                  ? State.Finish
                                  : State.ReadNumber;
                            break;

                        case State.Finish:
                            foreach (var writer in parts.Values)
                                writer.Dispose();

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

            ContinueNumberReading,
            ContinueStringFirstByteReading,
            ContinueStringSecondByteReading,
            ContinueStringTailReading,
            ReleaseDoubleBufferedLine,

            LoadNextBuff,
            CheckFinish,
            Finish
        }
    }
}
