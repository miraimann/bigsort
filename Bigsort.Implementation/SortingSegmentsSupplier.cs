using System;
using System.Diagnostics;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation
{
    public class SortingSegmentsSupplier
        : ISortingSegmentsSupplier
    {
        public const string
            LogName = nameof(SortingSegmentsSupplier),
            SupplingLogName = nameof(SupplyNext) + "." + LogName;
        
        private const int 
            SegmentSize = sizeof(ulong),
            BitsInByteCount = 8;
        
        private readonly ITimeTracker _timeTracker;
        private readonly Func<byte[], int, ulong> _read;
        private readonly int _usingBufferLength;

        public SortingSegmentsSupplier(
            IConfig config,
            IDiagnosticTools diagnosticTools = null)
        {
            _usingBufferLength = config.UsingBufferLength;
            _timeTracker = diagnosticTools?.TimeTracker;

            if (BitConverter.IsLittleEndian)
                 _read = ReverseRead;
            else _read = DirectRead;
        }

        public void SupplyNext(IGroup group)
        {
            var watch = Stopwatch.StartNew();

            var lines = group.Lines.Array;
            var segments = group.SortingSegments.Array;

            int offset = group.Lines.Offset,
                count = group.Lines.Count,
                n = offset + count;

            for (; offset < n; ++offset)
            {
                var line = lines[offset];
                ulong segment;

                int maxLength = (line.sortByDigits
                                    ? line.digitsCount + 1
                                    : line.lettersCount) -
                                 line.sortingOffset;

                if (maxLength <= 0)
                {
                    line.sortingOffset = 0;
                    if (line.sortByDigits)
                        segment = Consts.SegmentDigitsOut;
                    else
                    {
                        segment = Consts.SegmentLettersOut;
                        line.sortByDigits = true;
                    }
                }
                else
                {
                    var lineReadingOffset = line.start
                                          + line.sortingOffset
                                          + (line.sortByDigits ? 1 : line.digitsCount + 3);
                    
                    int cellIndex = lineReadingOffset % _usingBufferLength,
                        bufferIndex = lineReadingOffset / _usingBufferLength;

                    var buffers = group.Buffers;
                    segment = _read(buffers.Array[buffers.Offset + bufferIndex], cellIndex);
                    var bufferLeftLength = _usingBufferLength - cellIndex;
                    if (bufferLeftLength < SegmentSize) // is broken to two buffers
                    {
                        var bitsOffset = (SegmentSize - bufferLeftLength) * BitsInByteCount;
                        segment = (segment >> bitsOffset) << bitsOffset;

                        if (++bufferIndex < buffers.Count)
                            segment |= _read(buffers.Array[buffers.Offset + bufferIndex], 0)
                                    << (bufferLeftLength * BitsInByteCount);
                    }

                    if (maxLength < SegmentSize)
                    {
                        var bitsOffset = (SegmentSize - maxLength) * BitsInByteCount;
                        segment = (segment >> bitsOffset) << bitsOffset;
                    }                                                          
                                                                               
                    line.sortingOffset += SegmentSize;                        
                }                                                              
                                                                               
                lines[offset] = line;                                          
                segments[offset] = segment;                                           
            }
            
            _timeTracker?.Add(SupplingLogName, watch.Elapsed);
        }
        
        private static ulong DirectRead(byte[] buff, int offset) =>
            BitConverter.ToUInt64(buff, offset);

        private static ulong ReverseRead(byte[] buff, int offset) =>
            Reverse(DirectRead(buff, offset));

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
