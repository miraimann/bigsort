using System;
using System.Collections.Generic;
using System.Linq;
using Bigsort.Implementation;

namespace Bigsort.Tests
{

    public partial class GrouperTests
    {
        public class TrivialGrouper
        {
            public IEnumerable<Group> SplitToGroups(string[] lines) =>
                lines.Select((line, i) => new {line, i})
                     .Aggregate(new {position = 0, lines = Enumerable.Empty<Tuple<int, string>>()},
                         (acc, o) => new
                         {
                             position = acc.position + o.line.Length + Consts.EndLineBytesCount,
                             lines = acc.lines.Concat(new[] {Tuple.Create(acc.position, o.line)})
                         })
                     .lines
                     .Select(o => new {position = o.Item1, line = o.Item2})
                     .Select(o => new {o.position, o.line, digitsCount = o.line.IndexOf((char) Consts.Dot)})
                     .Select(o => new {o.position, o.line, o.digitsCount, lettersCount = o.line.Length - o.digitsCount - 1})
                     .Select(o => new
                     {
                         id = o.digitsCount != o.line.Length - 1
                             ? o.line.Substring(o.digitsCount + 1, Math.Min(Consts.GroupIdLettersCount, o.lettersCount))
                             : string.Empty,
                     
                         line = new Group.Line(o.position, content:
                             Enumerable.Concat(new[] {(byte) o.lettersCount, (byte) o.digitsCount},
                                               o.line.Select(c => (byte) c))
                                       .ToArray())
                     })
                     .GroupBy(o => o.id)
                     .Select(o => new {id = Id(o.Key), lines = o.Select(x => x.line)})
                     .Select(o => new Group(o.id)
                     {
                         LinesCount = o.lines.Count(),
                         BytesCount = o.lines.Sum(x => x.Content.Length),
                         Lines = o.lines
                     });

            private static int Id(string key)
            {
                var id = 0;
                if (key.Length == 0)
                    return id;

                id = (key[0] - Consts.AsciiPrintableCharsOffset)
                   * (Consts.AsciiPrintableCharsCount + 1)
                   + 1;

                if (key.Length == 1)
                    return id;

                id += key[1];
                ++id;
                id -= Consts.AsciiPrintableCharsOffset;

                return id;
            }
        }
    }
}
