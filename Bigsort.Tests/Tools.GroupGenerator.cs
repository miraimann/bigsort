using System;
using System.Collections.Generic;
using Bigsort.Implementation;

namespace Bigsort.Tests
{
    public static partial class Tools
    { 
        public static class GroupLinesGenerator
        {
            public const int
                MaxNumberLength = byte.MaxValue - 1,
                MaxStringLength = byte.MaxValue,
                  MaxLineLength = MaxNumberLength
                                + MaxStringLength
                                + 3;

            private const byte
                DigitsShift = (byte) '0';

            public static IEnumerable<byte[]> Generate(
                string id, 
                int linesCount,
                int maxNumberLength,
                int maxStringLength)
            {
                var random = new Random();
            
                for (int i = 0; i < linesCount; i++)
                {
                    int numberLength = random.Next(1, maxNumberLength),
                        stringLength = random.Next(id.Length, maxStringLength),
                            lineLength = numberLength + stringLength + 3;
                    
                    var buff = new byte[MaxLineLength];
                    random.NextBytes(buff);
                    buff[0] = (byte) stringLength;
                    buff[1] = (byte) numberLength;
                    buff[2] = (byte) (random.Next(1, 9) + DigitsShift);

                    int j = 3;
                    for (; j < numberLength + 2; j++) 
                        buff[j] = (byte) (buff[j] % 10 + DigitsShift);

                    buff[j++] = Consts.Dot;

                    for (int k = 0; k < id.Length; k++, j++)
                        buff[j] = (byte) id[k];

                    for (; j < lineLength; j++)
                        buff[j] = (byte) (buff[j] % 95 + 32);

                    Array.Resize(ref buff, lineLength);
                    yield return buff;
                }
            }
        }
    }
}
