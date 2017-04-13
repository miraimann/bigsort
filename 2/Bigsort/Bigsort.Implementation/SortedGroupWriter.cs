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

        public void Write(IGroupBytesMatrix group, 
                          Range linesRange, 
                          IWriter output)
        {
            var lines = _linesStorage.Indexes;

            var rows = group.Rows;
            int rowLength = group.RowLength,
                rowsCount = group.RowsCount,
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
                    line.start + lineLength == group.BytesCount;

                var row = rows[i];
                if (rowLeftLength < lineLength)
                {
                    var next = i + 1;
                    if (next < rowsCount)
                    {
                        output.Write(row, j, rowLeftLength);

                        var nextlength = lineLength - rowLeftLength;
                        var nextRow = rows[next];

                        if (!isLastLineInGroup)
                            nextRow[nextlength - 1] = Consts.EndLineByte2;
                        
                        output.Write(nextRow, 0, nextlength);
                        continue;
                    }
                }

                if (isLastLineInGroup)
                {
                    output.Write(row, j, lineLength - 2);
                    output.Write(Consts.EndLineBytes, 0,
                        Consts.EndLineBytes.Length);
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
