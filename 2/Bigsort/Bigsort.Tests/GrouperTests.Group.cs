using System.Collections.Generic;
using System.Linq;

namespace Bigsort.Tests
{
    public partial class GrouperTests
    {
        public class Group
        {
            public Group(int id)
            {
                Id = id;
            }

            public int Id { get; }
            public int BytesCount { get; internal set; }
            public int LinesCount { get; internal set; }
            public IEnumerable<Line> Lines { get; internal set; }

            public override string ToString() =>
                $"{Id}|{LinesCount}|{BytesCount}";

            public override bool Equals(object obj) =>
                this == obj as Group;

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
            public static bool operator ==(Group x, Group y) =>
                ((object) x == null && (object) y == null) ||
                ((object) x != null && (object) y != null
                    && x.Id == y.Id
                    && x.BytesCount == y.BytesCount
                    && x.LinesCount == y.LinesCount
                    && Enumerable.Zip(x.Lines.OrderBy(o => o.Position),
                                      y.Lines.OrderBy(o => o.Position),
                                      (a, b) => a == b)
                                 .All(o => o));

            public static bool operator !=(Group x, Group y) =>
                !(x == y);

            public static bool IsValid(Group x) =>
                   x.LinesCount == x.Lines.Count()
                && x.BytesCount == x.Lines.Sum(o => o.Content.Length);

            public static Group Zero { get; } =
                new Group(-1)
                {
                    Lines = Enumerable.Empty<Line>(),
                    BytesCount = 0,
                    LinesCount = 0
                };

            public class Line
            {
                public Line(long position, byte[] content)
                {
                    Position = position;
                    Content = content;
                }

                public byte[] Content { get; }
                public long Position { get; }

                public override bool Equals(object obj) =>
                    this == obj as Line;

                public override int GetHashCode() =>
                    (int)Position;

                public override string ToString() =>
                    $"[{Position:000}|{Content[0]:000}|{Content[1]:000}]" +
                    $"{new string(Content.Skip(2).Select(o => (char) o).ToArray())}";

                public static bool operator ==(Line x, Line y) =>
                    ((object) x == null && (object) y == null) ||
                    ((object) x != null && (object) y != null
                     // && x.Position == y.Position
                        && x.Content.Length == y.Content.Length
                        && Enumerable.Zip(x.Content, y.Content, (a, b) => a == b)
                                     .All(o => o));

                public static bool operator !=(Line x, Line y) =>
                    !(x == y);
            }
        }
    }
}
