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

        public void Write(IGroupBytes group, 
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
                     output.Write(row, j, rowLeftLength);
                     output.Write(rows[i + 1], 0, lineLength - rowLeftLength);
                }
                else output.Write(row, j, lineLength - rowLeftLength);                
            }

            output.Write(Consts.EndLineByte1);
            output.Write(Consts.EndLineByte2);
            output.Flush();
        }
    }
}
