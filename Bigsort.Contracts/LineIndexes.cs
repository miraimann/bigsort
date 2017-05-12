using System;
using System.Runtime.InteropServices;

namespace Bigsort.Contracts
{

    [StructLayout(LayoutKind.Explicit)]
    internal struct LineIndexes : IEquatable<LineIndexes>
    {
        public const byte DefaultSortingOffset = 2;
        public const bool DefaultSortByDigits = false;

        [FieldOffset(0)] private readonly int _asLong;

        [FieldOffset(0)] public int Start;
        [FieldOffset(4)] public byte DigitsCount;
        [FieldOffset(5)] public byte LettersCount;
        [FieldOffset(6)] public byte SortingOffset;

        [FieldOffset(7)] private byte _sortByDigits;
        public bool SortByDigits
        {
            get { return _sortByDigits != 0; }
            set { _sortByDigits = (byte)(value ? 1 : 0); }
        }

        public override string ToString() =>
            $"{Start}|{LettersCount}|{DigitsCount}|" +
            $"{SortingOffset}|{SortByDigits}";

        public override bool Equals(object obj) =>
            obj is LineIndexes
                && Equals((LineIndexes)obj);

        public bool Equals(LineIndexes other) =>
            _asLong == other._asLong;

        public override int GetHashCode() =>
            _asLong.GetHashCode();

        public static bool operator ==(
                LineIndexes left, LineIndexes right) =>
            left.Equals(right);

        public static bool operator !=(
                LineIndexes left, LineIndexes right) =>
            !left.Equals(right);
    }
}
