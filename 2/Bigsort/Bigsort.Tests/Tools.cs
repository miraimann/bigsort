using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Bigsort.Tests
{
    public static class Tools
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

        public static HashedBytesArray Hash(byte[] array) =>
            new HashedBytesArray(array);

        public class HashedBytesArray
            : IEquatable<HashedBytesArray>
        {
            public HashedBytesArray(byte[] value)
            {
                Value = value;
            }

            public byte[] Value { get; }

            public bool Equals(HashedBytesArray other) =>
                Value.Length == other.Value.Length
                && Enumerable.Zip(Value, other.Value, (x, y) => x == y)
                             .All(o => o);

            public override bool Equals(object obj) =>
                Equals((HashedBytesArray) obj);

            public override int GetHashCode() =>
                Value.Length >= sizeof(int)
                    ? BitConverter.ToInt32(Value, 0)
                    : Value.Reverse() // for little endian
                           .Select((x, i) => x * (int)Math.Pow(byte.MaxValue, i))
                           .Sum();
 
        }
    }
}
