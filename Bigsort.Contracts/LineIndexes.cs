using System;

namespace Bigsort.Contracts
{
    internal struct LineIndexes : IEquatable<LineIndexes>
    {
        public const byte DefaultSortingOffset = 2;
        public const bool DefaultSortByDigits = false;

        public int Start;
        public byte DigitsCount;
        public byte LettersCount;
        public byte SortingOffset;
        public bool SortByDigits;

        public override string ToString() =>
            $"{Start}|{LettersCount}|{DigitsCount}|" +
            $"{SortingOffset}|{SortByDigits}";

        public override bool Equals(object obj) =>
            obj is LineIndexes
                && Equals((LineIndexes)obj);
        
        public bool Equals(LineIndexes other) => 
               Start == other.Start
            && DigitsCount == other.DigitsCount
            && LettersCount == other.LettersCount
            && SortingOffset == other.SortingOffset
            && SortByDigits == other.SortByDigits;

        public override int GetHashCode() =>
            Start.GetHashCode();

        public static bool operator ==(
                LineIndexes left, LineIndexes right) =>
            left.Equals(right);

        public static bool operator !=(
                LineIndexes left, LineIndexes right) =>
            !left.Equals(right);
    }
}
