using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GroupMatrixService
        : IGroupMatrixService
    {
        private readonly IBuffersPool _buffersPool;
        private readonly int _rowLength;

        public GroupMatrixService(
            IBuffersPool buffersPool,
            IConfig config)
        {
            _buffersPool = buffersPool;
            _rowLength = config.BufferSize
                       - config.GroupBufferRowReadingEnsurance;
        }

        public int RowsCountFor(int bytesCount) =>
            (int) Math.Ceiling((double) bytesCount / _rowLength);

        public bool TryCreateMatrix(IGroupInfo groupInfo, out IGroupMatrix matrix)
        {
            var rowsCount = RowsCountFor(groupInfo.BytesCount);
            var rows = _buffersPool.TryGetBuffers(rowsCount);
            if (rows == null)
            {
                matrix = null;
                return false;
            }
            
            matrix = new Matrix(_rowLength, groupInfo, rows);
            return true;
        }

        public void LoadGroupToMatrix(
            IGroupMatrix matrix,
            IGroupInfo groupInfo,
            IFileReader groupsFileReader)
        {
            using (var rowIndex = Enumerable
                .Range(0, matrix.RowsCount)
                .GetEnumerator())
            {
                rowIndex.MoveNext();
                var loadingRow = matrix.Rows[rowIndex.Current];
                var positionInRow = 0;

                foreach (var blockRange in groupInfo.Mapping)
                {
                    groupsFileReader.Position = blockRange.Offset;
                    var blockOverPosition = groupsFileReader.Position + blockRange.Length;

                    while (groupsFileReader.Position != blockOverPosition)
                    {
                        var readLength = Math.Min(
                            (int) (blockOverPosition - groupsFileReader.Position),
                            matrix.RowLength - positionInRow);

                        readLength = groupsFileReader
                            .Read(loadingRow, positionInRow, readLength);

                        positionInRow += readLength;
                        if (positionInRow == matrix.RowLength && rowIndex.MoveNext())
                        {
                            loadingRow = matrix.Rows[rowIndex.Current];
                            positionInRow = 0;
                        }
                    }
                }
            }
        }

        private class Matrix
            : IGroupMatrix
        {
            private readonly Action _dispose;

            public Matrix(
                int rowLength,
                IGroupInfo groupInfo,
                IUsingHandle<byte[][]> rows)
            {
                BytesCount = groupInfo.BytesCount;
                LinesCount = groupInfo.LinesCount;

                RowLength = rowLength;
                Rows = rows.Value;
                RowsCount = Rows.Length;
                _dispose = rows.Dispose;
            }

            public byte[][] Rows { get; }
            public int LinesCount { get; }
            public int BytesCount { get; }
            public int RowsCount { get; }
            public int RowLength { get; }

            int IReadOnlyCollection<byte>.Count =>
                BytesCount;

            public byte this[int i]
            {
                get { return Rows[i/RowLength][i%RowLength]; }
                set { Rows[i/RowLength][i%RowLength] = value; }
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
