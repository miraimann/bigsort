using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class SortedGroupWriter
        : ISortedGroupWriter
    {
        private readonly ILinesIndexesStorage _linesStorage;

        public SortedGroupWriter(ILinesIndexesStorage linesStorage)
        {
            _linesStorage = linesStorage;
        }

        public void Write(IGroupMatrix group, 
                          Range linesRange, 
                          IFileWriter output)
        {
            var lines = _linesStorage.Indexes;

            var rows = group.Rows;
            int rowLength = group.RowLength,
                bytesCount = group.BytesCount,
                offset = linesRange.Offset,
                n = offset + linesRange.Length;
            
            while (offset < n)
            {
                var line = lines[offset++];
                int lineLength = line.digitsCount + line.lettersCount + 3,
                    start = line.start + 2,
                    i = start / rowLength,
                    j = start % rowLength,
                    rowLeftLength = rowLength - j;

                bool isLastLineInGroup =
                    line.start + lineLength == bytesCount;

                var row = rows[i];
                if (rowLeftLength < lineLength)
                {
                    var nextLength = lineLength - rowLeftLength;
                    if (isLastLineInGroup)
                    {
                        nextLength = nextLength - Consts.EndLineBytesCount;
                        if (nextLength <= 0)
                            output.Write(row, j, rowLeftLength + nextLength);
                        else
                        {
                            output.Write(row, j, rowLeftLength);
                            output.Write(rows[i + 1], 0, nextLength);
                        }

                        output.Write(Consts.EndLineBytes, 0, Consts.EndLineBytesCount);
                        continue;
                    }
                    
                    var nextRow = rows[i + 1];
                    output.Write(row, j, rowLeftLength);
                    nextRow[nextLength - 1] = Consts.EndLineByte2;
                    output.Write(nextRow, 0, nextLength);
                    continue;
                }

                if (isLastLineInGroup)
                {
                    output.Write(row, j, lineLength - Consts.EndLineBytesCount);
                    output.Write(Consts.EndLineBytes, 0, Consts.EndLineBytesCount);
                }
                else
                {
                    row[j + lineLength - 1] = Consts.EndLineByte2;
                    output.Write(row, j, lineLength);
                }
            }
            
            output.Flush();
        }
    }
}
