using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class LinesReservation<TSegment>
        : ILinesReservation<TSegment>
    {
        private readonly object o = new object();
        private readonly LinkedList<Range> _free = 
            new LinkedList<Range>();

        private readonly IUsingHandleMaker _disposableValueMaker; 
        private readonly IConfig _config;

        public LinesReservation(
            IUsingHandleMaker disposableValueMaker,
            IConfig config)
        {
            _disposableValueMaker = disposableValueMaker;
            _config = config;
        }

        public int Length { get; private set; }
        public LineIndexes[] Indexes { get; private set; }
        public TSegment[] Segments { get; private set; }

        public void Load()
        {
            var lineSize = Marshal.SizeOf<LineIndexes>() 
                         + Marshal.SizeOf<TSegment>();

            Length = (int) (_config.MaxMemoryForLines/lineSize);
            Indexes = new LineIndexes[Length];
            Segments = new TSegment[Length];

            _free.AddFirst(new Range(0, Length));
        }

        public IUsingHandle<Range> TryReserveRange(int length)
        {
            const int notFound = -1;
            int offset = notFound;
            lock(o)
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

            if (offset == notFound)
                return null;

            return _disposableValueMaker.Make(
                new Range(offset, length),
                _ =>
                {
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
                                    else link.Value = new Range(
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
                    }
                });
        }
    }
}
