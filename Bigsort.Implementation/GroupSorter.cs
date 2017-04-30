using System;
using System.Diagnostics;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation
{
    public class GroupSorter
        : IGroupSorter
    {
        public const string
            LogName = nameof(GroupSorter),
            SortingLogName = LogName + "." + nameof(Sort);

        private readonly ITimeTracker _timeTracker;
        private readonly ISortingSegmentsSupplier _segmentsSupplier;
        private readonly ILinesIndexesExtractor _linesIndexesExtractor;
        
        public GroupSorter(
            ISortingSegmentsSupplier segmentsSupplier,
            ILinesIndexesExtractor linesIndexesExtractor,
            IDiagnosticTools diagnosticTools = null)
        {
            _segmentsSupplier = segmentsSupplier;
            _linesIndexesExtractor = linesIndexesExtractor;
            _timeTracker = diagnosticTools?.TimeTracker;
        }

        public void Sort(IGroup group)
        {
            var watch = Stopwatch.StartNew();

            _linesIndexesExtractor.ExtractIndexes(group);
            Sort(group, group.Lines.Offset, group.Lines.Count);

            _timeTracker?.Add(SortingLogName, watch.Elapsed);
        }

        private void Sort(IGroup group, int offset, int length)
        {
            var lines = group.Lines.Array;
            var segments = group.SortingSegments.Array;

            _segmentsSupplier.SupplyNext(group);
            Array.Sort(segments, lines, offset, length);

            int n = offset + length;
            while (offset < n)
            {
                int i = offset;
                ulong current = segments[i], next = segments[++i];
                while (i < n && current.Equals(next) &&
                       !next.Equals(Consts.SegmentDigitsOut))
                    next = segments[++i];

                i -= offset;
                if (i != 1) Sort(group, offset, i);
                offset += i;
            }
        }
    }
}