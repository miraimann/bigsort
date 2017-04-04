using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bigsort.Tools.TestFileGenerator
{
    public static class Generator
    {
        private const byte
            DigitsShift = (byte)'0',
            Dot = (byte)'.';

        private static readonly List<string> MemoryUnits =
            new List<string> { "b", "Kb", "Mb", "Gb" };

        private static readonly byte[] End =
            Environment.NewLine.Select(o => (byte)o).ToArray();

        private static readonly Random Random = new Random();

        public static int Generate(
            string sizeData,     // "12345_Kb"  | "12_Mb"
            string lineSettings, // "[3].[789]" | "[3-7].[7-787]"
            string path)
        {
         
            // { "12345", "Kb" } | { "12", "Mb" }
            var sizeAndUnit = Split(sizeData, '_');

            var size = int.Parse(sizeAndUnit[0]);
            var unit = MemoryUnits.IndexOf(sizeAndUnit[1]);
            var maxSize = (long)(size * Math.Pow(1024, unit));

            // { "3", "789" } | { "3-7", "7-787" }
            var parts = Split(lineSettings, '.')
                .Select(WithoutPerenthesis)
                .ToArray();

            string
                numberData = parts[0], // "[3]" | "[3-7]"
                stringData = parts[1]; // "[789]" | "[7-787]"

            // { "3" } | { "3", "7" }
            var numberSettings = Split(numberData, '-');

            Func<byte[]> generateNumber, generateString;

            var x = int.Parse(numberSettings[0]);
            if (numberSettings.Length == 1)
                generateNumber = () => GenerateNumberWith(length: x);
            else
            {
                var y = int.Parse(numberSettings[1]);
                generateNumber = () => GenerateNumberWithLengthInRange(from: x, to: y);
            }

            // { "3" } | { "3", "7" }
            var stringSettings = Split(stringData, '-');

            var a = int.Parse(stringSettings[0]);
            if (stringSettings.Length == 1)
                generateString = () => GenerateStringWith(length: a);
            else
            {
                var b = int.Parse(stringSettings[1]);
                generateString = () => GenerateStringWithLengthInRange(from: a, to: b);
            }
            
            using (var stream = File.OpenWrite(path))
                while (true)
                {
                    var num = generateNumber();
                    var str = generateString();

                    var nextFileSize =
                        stream.Length + num.Length
                                      + str.Length
                                      + End.Length
                                      + 1; // dot

                    if (nextFileSize > maxSize)
                        break;

                    stream.Write(num, 0, num.Length);
                    stream.WriteByte(Dot);
                    stream.Write(str, 0, str.Length);
                    stream.Write(End, 0, End.Length);
                }

            return 0;
        }

        private static byte[] GenerateNumberWith(int length) =>
            GenerateBytesWith(length, filler: FillNumber);

        private static byte[] GenerateNumberWithLengthInRange(int from, int to) =>
            GenerateBytesWithLengthInRange(from, to, filler: FillNumber);

        private static void FillNumber(byte[] buff)
        {
            Random.NextBytes(buff);
            for (int i = 0; i < buff.Length; i++)
                buff[i] = (byte)(buff[i] % 10 + DigitsShift);
        }

        private static byte[] GenerateStringWith(int length) =>
            GenerateBytesWith(length, filler: FillString);

        private static byte[] GenerateStringWithLengthInRange(int from, int to) =>
            GenerateBytesWithLengthInRange(from, to, filler: FillString);

        private static void FillString(byte[] buff)
        {
            Random.NextBytes(buff);
            for (int i = 0; i < buff.Length; i++)
                buff[i] = (byte)(buff[i] % 95 + 32);
        }

        private static byte[] GenerateBytesWith(int length, Action<byte[]> filler)
        {
            var buff = new byte[length];
            filler(buff);
            return buff;
        }

        private static byte[] GenerateBytesWithLengthInRange(
                int from, int to, Action<byte[]> filler) =>
            GenerateBytesWith(Random.Next(from, to), filler);

        private static string[] Split(string source, char separator) =>
            source.Split(new[] { separator },
                         StringSplitOptions.RemoveEmptyEntries);

        private static string WithoutPerenthesis(string x) =>
            new string(x.ToCharArray(1, x.Length - 2));
    }
}
