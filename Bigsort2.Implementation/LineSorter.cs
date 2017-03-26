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
                       end = (byte) '\n';

            var inputReadCount = 0L;
            
            byte[] input = new byte[buffLength],
                   line = new byte[buffLength];

            var parts = new Dictionary<ushort, IWritingStream>(maxPartsCount);

            using (var inputStream = File.OpenRead(inputPath))
            {
                while ((inputReadCount =
                    inputStream.Read(input, 0, buffLength)) != 0)
                {
                    int i = 0;
                    byte digitsCount;

                    while (input[++i] != dot) ;
                    Array.Copy(input, 0, line, 1, i);
                    digitsCount = (byte) i;

                    ushort id = 0;
                    var first = input[++i];
                    if (first != end)
                    {
                        var second = input[++i];
                        if (second == end)
                            id = first;
                        else
                        {
                            if (BitConverter.IsLittleEndian)
                            {
                                input[i - 1] = second;
                                input[i] = first;
                            }

                            id = BitConverter.ToUInt16(input, i - 1);
                        }
                    }

                    if (!parts.ContainsKey(id))
                    {
                        var name = id.ToString(_partFileNameMask);
                        parts.Add(id, _ioService.OpenWrite(name));
                    }

                    var part = parts[id];
                    part.Write(digitsCount);
                    part.Write(input, 0, digitsCount);
                }
            }
        }

        private enum State
        {
            NumberReading,
            StringReading
        }
    }
}
