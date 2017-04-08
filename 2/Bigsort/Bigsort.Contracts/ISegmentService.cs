using System;

namespace Bigsort.Contracts
{
    public interface ISegmentService<TSegment>
        where TSegment : IEquatable<TSegment> 
                       , IComparable<TSegment>
    {
        byte SegmentSize { get; }

        TSegment LettersOut { get; }
        TSegment DigitsOut { get; }
        
        TSegment ShiftLeft(TSegment value, int bytesCount);

        TSegment ShiftRight(TSegment value, int bytesCount);

        TSegment Merge(TSegment a, TSegment b);

        TSegment Read(byte[] buff, int offset);
    }
}
