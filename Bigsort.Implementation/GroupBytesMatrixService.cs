using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GroupBytesMatrixService
        : IGroupBytesMatrixService
    {
        private readonly IBuffersPool _buffersPool;
        private readonly IConfig _config;

        public GroupBytesMatrixService(
            IBuffersPool buffersPool,
            IConfig config)
        {
            _buffersPool = buffersPool;
            _config = config;
        }

        public IGroupBytesMatrixRowsInfo CalculateRowsInfo(int bytesCount) =>
            new RowsInfo(bytesCount, _config.BufferSize
                                   - _config.GroupBufferRowReadingEnsurance);

        public IGroupBytesMatrix LoadMatrix(
                IGroupBytesMatrixRowsInfo rowsInfo,
                IGroupInfo groupInfo,
                IFileReader groupsFileReader) =>

            new Matrix(rowsInfo, groupInfo, groupsFileReader,
                _buffersPool);

        private class Matrix
            : IGroupBytesMatrix
        {
            private readonly Action _dispose;

            public Matrix(
                IGroupBytesMatrixRowsInfo rowsInfo,
                IGroupInfo groupInfo,
                IFileReader groupsFileReader,
                IBuffersPool buffersPool)
            {
                RowsCount = rowsInfo.RowsCount;
                RowLength = rowsInfo.RowLength;
                BytesCount = groupInfo.BytesCount;
                LinesCount = groupInfo.LinesCount;
                Rows = new byte[RowsCount][];
                
                using (var rowIndex = Enumerable
                            .Range(0, RowsCount)
                            .GetEnumerator())
                {
                    var handle = buffersPool.GetBuffer();
                    _dispose += handle.Dispose;
                    rowIndex.MoveNext();
                    var loadingRow = Rows[rowIndex.Current] = handle.Value;
                    var positionInRow = 0;
                    
                    foreach (var blockRange in groupInfo.Mapping)
                    {
                        groupsFileReader.Position = blockRange.Offset;
                        var blockOverPosition = groupsFileReader.Position + blockRange.Length;
                        
                        while (groupsFileReader.Position != blockOverPosition)
                        {
                            var readLength = Math.Min(
                                (int)(blockOverPosition - groupsFileReader.Position),
                                RowLength - positionInRow);
                            
                            readLength = groupsFileReader
                                .Read(loadingRow, positionInRow, readLength);

                            positionInRow += readLength;
                            if (positionInRow == RowLength && rowIndex.MoveNext())
                            {
                                handle = buffersPool.GetBuffer();
                                _dispose += handle.Dispose;
                                loadingRow = Rows[rowIndex.Current] = handle.Value;
                                positionInRow = 0;
                            }
                        }
                    }
                }
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
                _dispose?.Invoke();
        }

        private class RowsInfo
            : IGroupBytesMatrixRowsInfo
        {
            public RowsInfo(int bytesCount, int rowLength)
            {
                RowLength = rowLength;
                RowsCount = (int) Math.Ceiling((double) bytesCount/RowLength);
            }

            public int RowsCount { get; }
            public int RowLength { get; }
        }
    }
}
