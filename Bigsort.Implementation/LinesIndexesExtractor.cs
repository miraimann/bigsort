using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class LinesIndexesExtractor
        : ILinesIndexesExtractor
    {
        private readonly int _usingBufferLength;
        
        public LinesIndexesExtractor(IConfig config)
        {
            _usingBufferLength = config.UsingBufferLength;
        }

        public void ExtractIndexes(IGroup group)
        {
            var buffers = group.Buffers.Array;
            var lines = group.Lines.Array;

            int bufferLength = _usingBufferLength,
                buffersOffset = group.Buffers.Offset,
                linesOffset = group.Lines.Offset,
                n = linesOffset + group.Lines.Count, 
                i = 0;

            while (linesOffset < n)
            {
                int d = i / bufferLength + buffersOffset, 
                    b = i % bufferLength, 
                    q = (i + 1) / bufferLength + buffersOffset,
                    p = (i + 1) % bufferLength; 
                 
                var line = lines[linesOffset++] = new LineIndexes
                {
                    Start = i,
                    LettersCount = buffers[d][b],
                    DigitsCount  = buffers[q][p],
                    SortingOffset = Consts.GroupIdLettersCount
                };

                buffers[d][b] = Consts.EndLineByte1;
                
                i += line.DigitsCount;
                i += line.LettersCount;
                i += 3;
            }
        }
    }
}
