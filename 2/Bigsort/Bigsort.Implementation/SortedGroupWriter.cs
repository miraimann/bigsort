using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class SortedGroupWriter
        : ISortedGroupWriter
    {
        private readonly LineIndexes[] _lines;
        public SortedGroupWriter(ILinesIndexesStorage linesStorage)
        {
            _lines = linesStorage.Indexes;
        }

        public void Write(IGroupBytesMatrix group, 
                          Range linesRange, 
                          IWriter output)
        {
            var rows = group.Rows;
            int rowLength = group.RowLength,
                offset = linesRange.Offset,
                n = offset + linesRange.Length;
            
            for (; offset < n; ++offset)
            {
                var line = _lines[offset];
                int lineLength = line.digitsCount + line.lettersCount + 3,
                    i = line.start / rowLength,
                    j = line.start % rowLength,
                    rowLeftLength = rowLength - j;

                if (offset == linesRange.Offset)
                    j += 2;

                var row = rows[i];
                if (rowLeftLength < lineLength)
                {
                    var nextRow = rows[i + 1];
                    if (rowLeftLength > 1)
                         row[j + 1] = Consts.EndLineByte2;
                    else nextRow[0] = Consts.EndLineByte2;

                    output.Write(row, j, rowLeftLength);
                    output.Write(nextRow, 0, lineLength - rowLeftLength);
                }
                else
                {
                    row[j + 1] = Consts.EndLineByte2;
                    output.Write(row, j, lineLength - rowLeftLength);
                }                
            }

            output.Write(Consts.EndLineByte1);
            output.Write(Consts.EndLineByte2);
            output.Flush();
        }
    }
}
