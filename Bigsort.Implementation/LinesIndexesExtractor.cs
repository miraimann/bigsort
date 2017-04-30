using System.Diagnostics;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation
{
    public class LinesIndexesExtractor
        : ILinesIndexesExtractor
    {
        public const string
            LogName = nameof(LinesIndexesExtractor),
            IndexesExtractingLogName = LogName + "." + nameof(IndexesExtractingLogName);

        private readonly ITimeTracker _timeTracker;
        private readonly int _usingBufferLength;
        
        public LinesIndexesExtractor(
            IConfig config,
            IDiagnosticTools diagnosticTools = null)
        {
            _usingBufferLength = config.UsingBufferLength;
            _timeTracker = diagnosticTools?.TimeTracker;
        }

        public void ExtractIndexes(IGroup group)
        {
            var watch = Stopwatch.StartNew();

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
                    start = i,
                    lettersCount = buffers[d][b],
                    digitsCount  = buffers[q][p],
                    sortingOffset = Consts.GroupIdLettersCount
                };

                buffers[d][b] = Consts.EndLineByte1;
                
                i += line.digitsCount;
                i += line.lettersCount;
                i += 3;
            }

            _timeTracker?.Add(IndexesExtractingLogName, watch.Elapsed);
        }
    }
}
