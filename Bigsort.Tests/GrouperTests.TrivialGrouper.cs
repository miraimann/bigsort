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
            public Group[] SplitToGroups(IEnumerable<string> lines) =>
                lines.Select((line, i) => new {line, i})
                     .Aggregate(new {position = 0, lines = new List<Tuple<int, string>>()},
                        (acc, o) =>
                        {
                            acc.lines.Add(Tuple.Create(acc.position, o.line));
                            return new
                            {
                                position = acc.position + o.line.Length + Consts.EndLineBytesCount,
                                acc.lines
                            };
                        })
                     .lines
                     .Select(o => new {position = o.Item1, line = o.Item2})
                     .Select(o => new {o.position, o.line, digitsCount = o.line.IndexOf((char) Consts.Dot)})
                     .Select(o => new {o.position, o.line, o.digitsCount, lettersCount = o.line.Length - o.digitsCount - 1})
                     .Select(o => new
                     {
                         id = o.lettersCount != 0
                            ? o.line.Substring(o.digitsCount + 1, Math.Min(Consts.GroupIdLettersCount, o.lettersCount))
                            : string.Empty,
                     
                         line = new Group.Line(o.position, content:
                             Enumerable.Concat(new[] {(byte) o.lettersCount, (byte) o.digitsCount},
                                               o.line.Select(c => (byte) c))
                                       .ToArray())
                     })
                     .GroupBy(o => o.id)
                     .Select(o => new {id = ToId(o.Key), lines = o.Select(x => x.line)})
                     .Select(o => new Group(o.id)
                     {
                         LinesCount = o.lines.Count(),
                         BytesCount = o.lines.Sum(x => x.Content.Length),
                         Lines = o.lines
                     })
                     .OrderBy(o => o.Id)
                     .ToArray();
        }
#region DEBUG
// #if DEBUG
//         public Group[] SplitToGroupsDbg(IEnumerable<string> lines)
//         {
//             var _0 = lines.Select((line, i) => new { line, i });
//             var _1 = _0.Aggregate(new { position = 0, lines = Enumerable.Empty<Tuple<int, string>>() },
//                                  (acc, o) => new
//                                  {
//                                      position = acc.position + o.line.Length + Consts.EndLineBytesCount,
//                                      lines = acc.lines.Concat(new[] { Tuple.Create(acc.position, o.line) })
//                                  })
//                        .lines;
// 
//             var _2 = _1.Select(o => new { position = o.Item1, line = o.Item2 }).ToArray();
//             var _3 = _2.Select(o => new { o.position, o.line, digitsCount = o.line.IndexOf((char)Consts.Dot) }).ToArray();
//             var _4 = _3.Select(o => new { o.position, o.line, o.digitsCount, lettersCount = o.line.Length - o.digitsCount - 1 }).ToArray();
//             var _5 = _4.Select(o => new
//             {
//                 id = o.lettersCount != 0
//                                       ? o.line.Substring(o.digitsCount + 1, Math.Min(Consts.GroupIdLettersCount, o.lettersCount))
//                                       : string.Empty,
// 
//                 line = new Group.Line(o.position, content:
//                                       Enumerable.Concat(new[] { (byte)o.lettersCount, (byte)o.digitsCount },
//                                               o.line.Select(c => (byte)c))
//                                           .ToArray())
//             }).ToArray();
// 
//             var _6 = _5.GroupBy(o => o.id).ToArray();
//             var _7 = _6.Select(o => new { id = ToId(o.Key), lines = o.Select(x => x.line) })
//                        .Select(o => new Group(o.id)
//                        {
//                            LinesCount = o.lines.Count(),
//                            BytesCount = o.lines.Sum(x => x.Content.Length),
//                            Lines = o.lines
//                        }).ToArray();
//             var _8 = _7.OrderBy(o => o.Id)
//                        .ToArray();
//             return _8;
//         }
// #endif
#endregion

    }
}
