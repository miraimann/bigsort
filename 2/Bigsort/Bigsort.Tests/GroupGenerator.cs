using System;
using System.IO;
using Bigsort.Contracts;

namespace Bigsort.Tests
{
    public static class GroupGenerator
    {
        public const int
            MaxNumberLength = byte.MaxValue - 1,
            MaxStringLength = byte.MaxValue,
              MaxLineLength = MaxNumberLength
                            + MaxStringLength
                            + 3;

        private const byte
            DigitsShift = (byte) '0',
                    Dot = (byte) '.';

        public static IGroupInfo Generate(
            string id, 
            string path, 
            int linesCount,
            int maxNumberLength,
            int maxStringLength)
        {
            var random = new Random();
            var buff = new byte[MaxLineLength]; 

            using (var stream = File.OpenWrite(path))
                for (int i = 0; i < linesCount; i++)
                {
                    int numberLength = random.Next(1, maxNumberLength),
                        stringLength = random.Next(0, maxStringLength),
                          lineLength = numberLength + stringLength + 3;

                    random.NextBytes(buff);
                    buff[0] = (byte) stringLength;
                    buff[1] = (byte) numberLength;
                    buff[2] = (byte) (random.Next(1, 9) + DigitsShift);

                    int j = 3;
                    for (; j < numberLength + 2; j++) 
                        buff[j] = (byte) (buff[j] % 10 + DigitsShift);

                    buff[j++] = Dot;

                    for (; j < id.Length; j++)
                        buff[j] = (byte) id[j];

                    for (; j < lineLength; j++)
                        buff[j] = (byte) (buff[j] % 95 + 32);

                    stream.Write(buff, 0, lineLength);
                }

            return new GroupInfo(id, linesCount, 
                (int) new FileInfo(path).Length);
        }
    }
}
