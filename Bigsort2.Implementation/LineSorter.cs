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
        public unsafe void Sort(string inputPath, string outputPath)
        {
            const int buffLength = 64 * 1024,
                      idLength = sizeof(long);

            const byte dot = (byte) '.', 
                       end = (byte) '\n';

            var inputReadCount = 0L;
            
            byte[] input = new byte[buffLength],
                   line = new byte[buffLength],
                   idBuff = new byte[idLength];

            var partsBuffsPositions = new Dictionary<long, int>(1024);
            var partsBuffs = new Dictionary<long, byte[]>(1024);
            
            using (var inputStream = File.OpenRead(inputPath))
            {
                while ((inputReadCount =
                    inputStream.Read(input, 0, buffLength)) != 0)
                {
                    int i = 0, j, k = 0;

                    // find dot index and number 
                    // bytes count = digits count
                    while (input[++i] != dot) ;

                    // set number digits count 
                    // to first line byte
                    line[0] = (byte) i;

                    // copy number
                    Array.Copy(input, 0, line, 1, i); 

                    var x = input[j = ++i];
                    Array.Clear(idBuff, 0, idLength);

                    if (BitConverter.IsLittleEndian)
                    {
                        // set 8 letters (8 bytes, 1 long) to idBuff
                        // (or letters count whicth is equel to line length, 
                        //  if line length less than 8)
                        // in reverce order, becouse Little Endian
                        k = idLength;
                        while (k > 0 && x != end)
                            idBuff[--k] = x = input[i++];

                        // if line length less than 8
                        if (k > 0) // set 0 to two bytes for letters count
                            line[j++] = line[j++] = 0;
                    }
                    else
                    {
                        // set 8 letters (8 bytes, 1 long) to idBuff
                        // (or letters count whicth is equel to line length, 
                        //  if line length less than 8)
                        while (k < idLength && x != end)
                            idBuff[k++] = x = input[i++];

                        // if line length less than 8
                        if (k < idLength) // set 0 to two bytes for letters count
                            line[j++] = line[j++] = 0;
                    }

                    var id = BitConverter.ToInt64(idBuff, 0);
                    if (!partsBuffs.ContainsKey(id))
                    {
                        partsBuffsPositions.Add(id, 0);
                        partsBuffs.Add(id, new byte[buffLength]);
                    }
                }
            }
        }
    }
}
