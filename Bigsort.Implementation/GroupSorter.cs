using System;
using System.Diagnostics;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation
{
    public class GroupSorter<TSegment>
        : IGroupSorter
        where TSegment : IEquatable<TSegment>
        , IComparable<TSegment>
    {
        public const string
            LogName = nameof(GroupSorter<TSegment>),
            SortingLogName = LogName + "." + nameof(Sort);

        private readonly ITimeTracker _timeTracker;

        private readonly ISortingSegmentsSupplier _segmentsSupplier;
        private readonly ILinesIndexesExtractor _linesIndexesExtractor;
        private readonly ILinesStorage<TSegment> _linesStorage;
        private readonly TSegment _lineSegmentsOut;

        public GroupSorter(
            ISortingSegmentsSupplier segmentsSupplier,
            ILinesIndexesExtractor linesIndexesExtractor,
            ILinesStorage<TSegment> linesStorage,
            ISegmentService<TSegment> segmentService,
            IDiagnosticTools diagnosticTools = null)
        {
            _segmentsSupplier = segmentsSupplier;
            _linesIndexesExtractor = linesIndexesExtractor;
            _linesStorage = linesStorage;
            _lineSegmentsOut = segmentService.DigitsOut;

            _timeTracker = diagnosticTools?.TimeTracker;
        }

        public void Sort(IGroup group)
        {
            var watch = Stopwatch.StartNew();

            _linesIndexesExtractor.ExtractIndexes(group);
            Sort(group, group.LinesRange.Offset, group.LinesRange.Length);

            _timeTracker?.Add(SortingLogName, watch.Elapsed);
        }

        private void Sort(IGroup group, int offset, int length)
        {
            var lines = _linesStorage.Indexes;
            var segments = _linesStorage.Segments;

            _segmentsSupplier.SupplyNext(group, offset, length);
            Array.Sort(segments, lines, offset, length);

            int n = offset + length;
            while (offset < n)
            {
                int i = offset;
                TSegment current = segments[i], next = segments[++i];
                while (i < n && current.Equals(next) &&
                       !next.Equals(_lineSegmentsOut))
                    next = segments[++i];

                i -= offset;
                if (i != 1) Sort(group, offset, i);
                offset += i;
            }
        }
    }
}