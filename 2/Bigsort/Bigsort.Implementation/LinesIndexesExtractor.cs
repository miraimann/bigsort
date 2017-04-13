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
                var line = lines[offset++] = new LineIndexes
                {
                    start = i,
                    lettersCount = group[i],
                    digitsCount = group[i + 1],
                    sortingOffset = 2,
                 // sortByDigits = false;
                };

                group[i] = Consts.EndLineByte1;
                // it is nessesary for sort
                // it will be written in SortedGroupWriter 
                // group[i + 1] = Consts.EndLineByte2;
                
                i += line.digitsCount;
                i += line.lettersCount;
                i += 3;
            }
        }
    }
}
