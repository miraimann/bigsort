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
                last = offset + length, 
                i = 0;

            while (offset <= last)
            {
                var line = lines[offset++];
                line.start = i;

                line.lettersCount = group[i];
                group[i] = Consts.EndLineByte1;

                line.digitsCount = group[++i];
                // it is nessesary for sort
                // it will be written in SortedGroupWriter 
                // group[i] = Consts.EndLineByte2;
                
                line.sortingOffset = 2;
                // line.sortByDigits = false;
                i += line.digitsCount;
                i += line.lettersCount;
                i += 3;
            }
        }
    }
}
