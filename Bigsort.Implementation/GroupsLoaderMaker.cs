using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GroupsLoaderMaker
        : IGroupsLoaderMaker
    {
        public IGroupsLoader Make(string groupFilePath, 
            IGroupsSummaryInfo groupsSummary, 
            IGroup[] output)
        {
            throw new NotImplementedException();
        }

        private class Group
            : IGroup
        {
            private readonly Action _dispose;

            public Group(
                int rowLength,
                GroupInfo groupInfo,
                IUsingHandle<Range> lines,
                IUsingHandle<byte[][]> rows)
            {
                BytesCount = groupInfo.BytesCount;
                LinesCount = groupInfo.LinesCount;

                LinesRange = lines.Value;
                Rows = rows.Value;

                RowLength = rowLength;
                RowsCount = Rows.Length;

                _dispose = lines.Dispose;
                _dispose += rows.Dispose;
            }

            public byte[][] Rows { get; }
            public Range LinesRange { get; }
            public int LinesCount { get; }
            public int BytesCount { get; }
            public int RowsCount { get; }
            public int RowLength { get; }

            int IReadOnlyCollection<byte>.Count =>
                BytesCount;

            public byte this[int i]
            {
                get { return Rows[i / RowLength][i % RowLength]; }
                set { Rows[i / RowLength][i % RowLength] = value; }
            }

            public IEnumerator<byte> GetEnumerator() =>
                Rows.Select(row => row.Take(RowLength))
                    .Aggregate(Enumerable.Concat)
                    .Take(BytesCount)
                    .GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() =>
                GetEnumerator();

            public void Dispose() =>
                _dispose();
        }
    }
}
