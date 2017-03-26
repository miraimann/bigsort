using Bigsort2.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bigsort2.Implementation
{
    public class LineSorter
        : ILinesSorter
    {
        private readonly string 
            _partFileNameMask,
            _partsDirecory;

        private readonly IIoService _ioService;
        private readonly IConfig _config;

        public LineSorter(IIoService ioService, IConfig config)
        {
            _ioService = ioService;
            _config = config;

            var ushortDigitsCount =
                (int) Math.Ceiling(Math.Log10(ushort.MaxValue));
            _partFileNameMask = new string('0', ushortDigitsCount);

            _partsDirecory = Path.Combine(
                _ioService.TempDirectory, 
                _config.PartsDirectory);
            
            _ioService.SetCurrentDirectory(_partsDirecory);
        }

        public unsafe void Sort(string inputPath, string outputPath)
        {
            const int buffLength = 64*1024,
                      maxPartsCount = 96 * 96 + 96 + 1;

            const byte dot = (byte) '.', 
                       endLine = (byte) '\n',
                       endStream = 0,
                       endBuff = 1;

            int lastBuffIndex = buffLength - 1;

            byte[] prevBuff = new byte[buffLength],
                   buff = new byte[buffLength],
                   idBuff = new byte[sizeof(ushort)];

            var parts = new Dictionary<ushort, IWriter>(maxPartsCount);

            using (var inputStream = File.OpenRead(inputPath))
            {
                int i = 1, j = 1, k = 0;

                while (true)
                {
                    byte digitsCount = 0;
                    ushort lettersCount = 0;
                        
                    int count = inputStream.Read(buff, i + 1, lastBuffIndex);
                    if (count == lastBuffIndex)
                        buff[lastBuffIndex] = endBuff;
                    else buff[count - 1] = endStream;

                    while (buff[++i] > dot) ;
                    if (buff[i] == dot)
                        buff[j] = digitsCount = (byte) (i - j);
                    else // if (c == endBuff)
                    {
                        k = i;
                        i = 0;

                        var tmp = buff;
                        buff = prevBuff;
                        prevBuff = tmp;

                        count = inputStream.Read(buff, 0, lastBuffIndex);
                        if (count == lastBuffIndex)
                            buff[lastBuffIndex] = endBuff;
                        else buff[count - 1] = endStream;
                        
                        while (buff[++i] > dot) ;
                        prevBuff[j] = digitsCount = (byte) (k - j + i);
                    }

                    var dotIndex = i++;
                    ushort id = 0;

                    while (true)
                    {
                        var c = buff[i];
                        if (c > endLine)
                        {
                            id = c;
                            ++i;

                            while (true)
                            {
                                c = buff[i];
                                if (c > endLine)
                                {
                                    id *= c;
                                    break;
                                }

                                if (c == endBuff)
                                {
                                    i = 0;

                                    var tmp = buff;
                                    buff = prevBuff;
                                    prevBuff = tmp;

                                    count = inputStream.Read(buff, 0, lastBuffIndex);
                                    if (count == lastBuffIndex)
                                        buff[lastBuffIndex] = endBuff;
                                    else buff[count - 1] = endStream;
                                }
                                else break; // buff[i] == endLine
                            }

                            break;
                        }

                        if (buff[i] == endBuff)
                        { 
                            i = 0;

                            var tmp = buff;
                            buff = prevBuff;
                            prevBuff = tmp;

                            count = inputStream.Read(buff, 0, lastBuffIndex);
                            if (count == lastBuffIndex)
                                buff[lastBuffIndex] = endBuff;
                            else buff[count - 1] = endStream;
                        } else break; // buff[i] == endLine
                    }

                    while (buff[++i] > endLine) ;
                    if (buff[i] == endLine)
                    {
                        if (dotIndex > i) // dot is in previous buff
                        {

                        }
                        else
                        {
                            lettersCount = (ushort) (i - dotIndex);
                            if (lettersCount <= sbyte.MaxValue)
                            {
                                if (BitConverter.IsLittleEndian)
                                {
                                    buff[j - 1] = (byte) lettersCount;
                                    buff[dotIndex] = 0;
                                }
                                else
                                {
                                    buff[j - 1] = 0;
                                    buff[dotIndex] = (byte) lettersCount;
                                }

                                if (lettersCount != 0)
                                {
                                    if (lettersCount == 1)
                                        id = buff[dotIndex + 1];
                                    else
                                    {
                                        
                                    }
                                }
                            }
                            else
                            {
                                var lettersCountBytes = 
                                    BitConverter.GetBytes(lettersCount);
                                buff[j - 1] = lettersCountBytes[0];
                                buff[dotIndex] = lettersCountBytes[1];
                            }
                            
                            if (lettersCount != 0)
                            {
                                
                            }
                                 
                            var first = buff[++i];
                            if (first != end)
                            {
                                var second = currentBuff[++i];
                                if (second == end)
                                    id = first;
                                else
                                {
                                    if (BitConverter.IsLittleEndian)
                                    {
                                        currentBuff[i - 1] = second;
                                        currentBuff[i] = first;
                                    }

                                    id = BitConverter.ToUInt16(currentBuff, i - 1);
                                }
                            }

                            //        if (!parts.ContainsKey(id))
                            //        {
                            //            var name = id.ToString(_partFileNameMask);
                            //            parts.Add(id, _ioService.OpenWrite(name));
                            //        }
                        }
                    }

                }

                //    while ((inputReadCount =
                //        inputStream.Read(currentBuff, 0, buffLength)) != 0)
                //    {
                //        int i = 0;
                //        byte digitsCount;

                //        while (currentBuff[++i] != dot) ;
                //        Array.Copy(currentBuff, 0, line, 1, i);
                //        digitsCount = (byte) i;

                //        ushort id = 0;
                //        var first = currentBuff[++i];
                //        if (first != end)
                //        {
                //            var second = currentBuff[++i];
                //            if (second == end)
                //                id = first;
                //            else
                //            {
                //                if (BitConverter.IsLittleEndian)
                //                {
                //                    currentBuff[i - 1] = second;
                //                    currentBuff[i] = first;
                //                }

                //                id = BitConverter.ToUInt16(currentBuff, i - 1);
                //            }
                //        }

                //        if (!parts.ContainsKey(id))
                //        {
                //            var name = id.ToString(_partFileNameMask);
                //            parts.Add(id, _ioService.OpenWrite(name));
                //        }

                //        var part = parts[id];
                //        part.Write(digitsCount);
                //        part.Write(currentBuff, 0, digitsCount);
                //    }
            }
        }

        private enum State
        {
            NumberReading,
            StringReading
        }
    }
}
