using System.Collections.Generic;
using System.Linq;

namespace Bigsort.Implementation
{
    public static class Dbg
    {
        public static string View(uint value) =>
            new string(Enumerable
                .Range(0, sizeof(uint))
                .Select(o => new
                    {
                        x = 0xFF000000 >> (o * 8),
                        i = sizeof(uint) - o - 1
                    })
                .Select(o => new { x = value & o.x, o.i })
                .Select(o => o.x >> (o.i * 8))
                .Select(x => (char) x)
                .Select(o => o == 0 ? '*' : o)
                .ToArray());

        public static string View(ulong value) =>
            new string(Enumerable
                .Range(0, sizeof(ulong))
                .Select(o => new
                    {
                        x = 0xFF00000000000000 >> (o * 8),
                        i = sizeof(ulong) - o - 1
                    })
                .Select(o => new { x = value & o.x, o.i })
                .Select(o => o.x >> (o.i * 8))
                .Select(o => (char)o)
                .Select(o => o == 0 ? '*' : o)
                .ToArray());

        public static string View(byte o) =>
            $"{(char)o}:{o}"; // $"{(char) o}"; 

        public static string View<T>(T[] array, int offset, int count) =>
            string.Join(", ", array.Skip(offset).Take(count)
                                   .Cast<object>().Select(View));

        public static string View(object o) =>
            o is byte
                ? View((byte) o)
                : (o is ulong
                    ? View((ulong) o)
                    : o is uint
                        ? View((uint) o)
                        : "?");
    }
}
