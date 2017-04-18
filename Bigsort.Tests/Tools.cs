using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Bigsort.Tests
{
    public static partial class Tools
    {
        public static byte[] BytesOfString_s(
                string[] lines, bool addEndLines = false) =>

            lines.SelectMany(l => BytesOfString(l, addEndLines))
                 .ToArray();
        
        public static byte[] BytesOfString(
                string line, bool addEndLine = false) =>

            line.Select(o => (byte) o)
                .Concat(addEndLine
                    ? Environment.NewLine.Select(o => (byte) o)
                    : Enumerable.Empty<byte>())
                .ToArray();

        public static string[] SplitString(string str, string separator) =>
            str.Split(new[] { separator },
                StringSplitOptions.RemoveEmptyEntries);

        public static IEnumerable<string> ReadAllLinesFrom(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var reader = new StreamReader(stream, Encoding.ASCII))
                while (!reader.EndOfStream)
                    yield return reader.ReadLine();
        }

        public static void Mix<T>(T[] source, int mixesCount)
        {
            var random = new Random();
            for (int i = 0; i < mixesCount; i++)
            {
                var j = random.Next(0, source.Length);
                var k = random.Next(0, source.Length);

                var temp = source[k];
                source[k] = source[j];
                source[j] = temp;
            }
        }
    }
}
