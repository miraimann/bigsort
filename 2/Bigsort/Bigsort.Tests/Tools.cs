using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    }
}
