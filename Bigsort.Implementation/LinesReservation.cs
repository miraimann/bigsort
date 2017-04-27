using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation
{
    public class LinesReservation<TSegment>
        : ILinesReservation<TSegment>
    {
        public const string
            LogName = nameof(LinesReservation<TSegment>),
            RangeReservingLogName = LogName + "." + nameof(TryReserveRange),
            RangeDisposingLogName = LogName + ".DisposeRange";

        private readonly ITimeTracker _timeTracker;

        private readonly object o = new object();
        private readonly LinkedList<Range> _free = 
            new LinkedList<Range>();
        
        private readonly IUsingHandleMaker _uingHandleMaker; 
        private readonly IConfig _config;

        public LinesReservation(
            IUsingHandleMaker uingHandleMaker,
            IConfig config,
            IDiagnosticTools diagnosticTools = null)
        {
            _timeTracker = diagnosticTools?.TimeTracker;

            _uingHandleMaker = uingHandleMaker;
            _config = config;

            LineSize = Marshal.SizeOf<LineIndexes>()
                     + Marshal.SizeOf<TSegment>();
        }

        public int Length { get; private set; }
        public LineIndexes[] Indexes { get; private set; }
        public TSegment[] Segments { get; private set; }

        public int LineSize { get; }

        public void Load(int capacity)
        {
            var lineSize = Marshal.SizeOf<LineIndexes>() 
                         + Marshal.SizeOf<TSegment>();

            Length = Math.Min(capacity,
                (int) (_config.MaxMemoryForLines/lineSize));
            
            Indexes = new LineIndexes[Length];
            Segments = new TSegment[Length];

            _free.AddFirst(new Range(0, Length));
        }

        public IUsingHandle<Range> TryReserveRange(int length)
        {
            var start = DateTime.Now;

            const int notFound = -1;
            int offset = notFound;
            lock (o)
            {
                var link = _free.First;
                while (link != null)
                {
                    if (link.Value.Length >= length)
                    {
                        offset = link.Value.Offset;
                        var cutedLength = link.Value.Length - length;
                        if (cutedLength == 0)
                            _free.Remove(link);
                        else
                            link.Value = new Range(
                                link.Value.Offset + length,
                                cutedLength);
                        break;
                    }

                    link = link.Next;
                }
            }

            _timeTracker?.Add(RangeReservingLogName,
                DateTime.Now - start);

            return offset == notFound
                ? null
                : _uingHandleMaker.Make(
                    new Range(offset, length),
                    delegate 
                    {
                        var rangeDisposingStart = DateTime.Now;

                        lock (o)
                        {
                            LinkedListNode<Range>
                                link = _free.First,
                                prev = null;

                            while (link != null)
                            {
                                if (link.Value.Offset > offset)
                                {
                                    if (offset + length == link.Value.Offset)
                                    {
                                        if (prev != null &&
                                            prev.Value.Offset +
                                            prev.Value.Length == offset)
                                        {
                                            var newLength = prev.Value.Length
                                                            + link.Value.Length
                                                            + length;

                                            prev.Value = new Range(
                                                prev.Value.Offset,
                                                newLength);

                                            _free.Remove(link);
                                        }
                                        else
                                            link.Value = new Range(
                                                link.Value.Offset - length,
                                                link.Value.Length + length);
                                    }
                                    else
                                    {
                                        if (prev != null &&
                                            prev.Value.Offset +
                                            prev.Value.Length == offset)
                                            prev.Value = new Range(
                                                prev.Value.Offset,
                                                prev.Value.Length + length);
                                        else
                                            _free.AddBefore(link,
                                                new Range(offset, length));
                                    }

                                    return;
                                }

                                prev = link;
                                link = link.Next;
                            }

                            _free.AddLast(new Range(offset, length));

                            _timeTracker?.Add(RangeDisposingLogName,
                                DateTime.Now - rangeDisposingStart);
                        }
                    });
        }
    }
}
