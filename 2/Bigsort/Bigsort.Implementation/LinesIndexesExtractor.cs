using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class LinesIndexesExtractor
        : ILinesIndexesExtractor
    {
        private readonly ILinesIndexesStorage _linesStorage;
        
        public LinesIndexesExtractor(
            ILinesIndexesStorage linesStorage)
        {
            _linesStorage = linesStorage;
        }

        public void ExtractIndexes(
            IFixedSizeList<byte> group, 
            Range linesRange)
        {
            var lines = _linesStorage.Indexes;
            int offset = linesRange.Offset,
                length = linesRange.Length,
                n = offset + length, 
                i = 0;

            while (offset < n)
            {
                var line = lines[offset];
                line.start = i;

                line.lettersCount = group[i];
                group[i] = Consts.EndLineByte1;

                line.digitsCount = group[++i];
                // it is nessesary for sort
                // it will be written in SortedGroupWriter 
                // group[i] = Consts.EndLineByte2;

                // line.sortByDigits = false;
                line.sortingOffset = 2;
                lines[offset] = line;
                i += line.digitsCount;
                i += line.lettersCount;
                i += 2;

                ++offset;
            }
        }
    }
}
