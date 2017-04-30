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
        
        public LinesIndexesExtractor(IDiagnosticTools diagnosticTools = null)
        {
            _timeTracker = diagnosticTools?.TimeTracker;
        }

        public void ExtractIndexes(IGroup group)
        {
            var watch = Stopwatch.StartNew();

            var lines = group.Lines.Array;
            int offset = group.Lines.Offset,
                length = group.Lines.Count,
                n = offset + length, 
                i = 0;

            while (offset < n)
            {
                var line = lines[offset++] = new LineIndexes
                {
                    start = i,
                    lettersCount = group[i],
                    digitsCount = group[i + 1],
                    sortingOffset = Consts.GroupIdLettersCount
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

            _timeTracker?.Add(IndexesExtractingLogName, watch.Elapsed);
        }
    }
}
