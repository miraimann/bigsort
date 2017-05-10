using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;
using static Bigsort.Implementation.Consts;

namespace Bigsort.Implementation
{
    internal class SortingSegmentsSupplier
        : ISortingSegmentsSupplier
    {
        public const string
            LogName = nameof(SortingSegmentsSupplier),
            SupplingLogName = nameof(SupplyNext) + "." + LogName;
        
        private static readonly Func<byte[], int, ulong> ReadSegment;

        private readonly ITimeTracker _timeTracker;
        private readonly int _usingBufferLength;

        static SortingSegmentsSupplier()
        {
            if (BitConverter.IsLittleEndian)
                ReadSegment = ReverseReadSegment;
            else ReadSegment = DirectReadSegment;
        }

        public SortingSegmentsSupplier(
            IConfig config,
            IDiagnosticTools diagnosticTools = null)
        {
            _usingBufferLength = config.UsingBufferLength;
            _timeTracker = diagnosticTools?.TimeTracker;
        }
        
        public void SupplyNext(IGroup group, int offset, int count)
        {
            var watch = Stopwatch.StartNew();

            var lines = group.Lines.Array;
            var segments = group.SortingSegments.Array;

            var n = offset + count;
            for (; offset < n; ++offset)
            {
                var line = lines[offset];
                ulong segment;

                int maxLength = (line.SortByDigits
                                    ? line.DigitsCount + 1
                                    : line.LettersCount) -
                                 line.SortingOffset;
                if (maxLength <= 0)
                {
                    line.SortingOffset = 0;
                    if (line.SortByDigits)
                        segment = SegmentDigitsOut;
                    else
                    {
                        segment = SegmentLettersOut;
                        line.SortByDigits = true;
                    }
                }
                else
                {
                    var buffers = group.Buffers;

                    int bufferLength = _usingBufferLength,
                        lineReadingOffset = line.Start + line.SortingOffset
                                          + (line.SortByDigits ? 1 : line.DigitsCount + 3),

                        cellIndex = lineReadingOffset % bufferLength,
                        buffIndex = lineReadingOffset / bufferLength;

                    segment = ReadSegment(buffers.Array[buffers.Offset + buffIndex], cellIndex);
                    var bufferRightLength = bufferLength - cellIndex;
                    if (bufferRightLength < SegmentSize) // is broken to two buffers
                    {
                        var bitsOffset = (SegmentSize - bufferRightLength) * BitsInByteCount;
                        segment = (segment >> bitsOffset) << bitsOffset;

                        if (++buffIndex < bufferLength)
                            segment |= ReadSegment(buffers.Array[buffers.Offset + buffIndex], 0)
                                    >> (bufferRightLength * BitsInByteCount);
                    }

                    if (maxLength < SegmentSize)
                    {
                        var bitsOffset = (SegmentSize - maxLength) * BitsInByteCount;
                        segment = (segment >> bitsOffset) << bitsOffset;
                    }

                    line.SortingOffset += SegmentSize;
                }

                lines[offset] = line;
                segments[offset] = segment;
            }
            
            _timeTracker?.Add(SupplingLogName, watch.Elapsed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong DirectReadSegment(byte[] buff, int offset) =>
            BitConverter.ToUInt64(buff, offset);
        
        private static ulong ReverseReadSegment(byte[] buff, int offset) =>
            Reverse(DirectReadSegment(buff, offset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Reverse(ulong x) =>
             ((x & 0xFF00000000000000) >> 56)
           | ((x & 0x00FF000000000000) >> 40)
           | ((x & 0x0000FF0000000000) >> 24)
           | ((x & 0x000000FF00000000) >> 8)
           | ((x & 0x00000000FF000000) << 8)
           | ((x & 0x0000000000FF0000) << 24)
           | ((x & 0x000000000000FF00) << 40)
           | ((x & 0x00000000000000FF) << 56);
    }
}
