using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class LinesIndexesExtractor
        : ILinesIndexesExtractor
    {   
        private readonly LineIndexes[] _lines;

        public LinesIndexesExtractor(
            ILinesIndexesStorage linesStorage)
        {
            _lines = linesStorage.Indexes;
        }

        public void ExtractIndexes(
            IFixedSizeList<byte> group, 
            Range linesRange)
        {
            int offset = linesRange.Offset,
                length = linesRange.Length,
                last = offset + length, 
                i = 0;

            while (offset <= last)
            {
                var line = _lines[offset++];
                line.start = i;

                line.lettersCount = group[i];
                group[i] = Consts.EndLineByte1;

                line.digitsCount = group[++i];
                group[i] = Consts.EndLineByte2;

                if (line.lettersCount <= 2)
                {
                    line.sortingOffset = 0;
                    line.sortByDigits = true;
                }
                else
                {
                    line.sortingOffset = line.lettersCount;
                    line.sortByDigits = false;
                }

                i += line.digitsCount;
                i += line.lettersCount;
                i += 3;
            }
        }
    }
}
