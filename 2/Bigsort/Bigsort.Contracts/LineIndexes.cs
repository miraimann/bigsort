using System;
using System.Runtime.InteropServices;

namespace Bigsort.Contracts
{
    [StructLayout(LayoutKind.Explicit, Size = sizeof(long))]
    public struct LineIndexes : IEquatable<LineIndexes>
    {
        public const byte DefaultSortingOffset = 2;
        public const bool DefaultSortByDigits = false;

        [FieldOffset(0)] private readonly long asLong;
        [FieldOffset(0)] public int start;
        [FieldOffset(4)] public byte digitsCount;
        [FieldOffset(5)] public byte lettersCount;
        [FieldOffset(6)] public byte sortingOffset;
        [FieldOffset(7)] public bool sortByDigits;

        public override string ToString() =>
            $"{start}|{lettersCount}|{digitsCount}|" +
            $"{sortingOffset}|{sortByDigits}";

        public override bool Equals(object obj) =>
            obj is LineIndexes
                && Equals((LineIndexes)obj);
        
        public bool Equals(LineIndexes other) =>
            asLong == other.asLong;
        
        public override int GetHashCode() =>
            asLong.GetHashCode();

        public static bool operator ==(
                LineIndexes left, LineIndexes right) =>
            left.Equals(right);

        public static bool operator !=(
                LineIndexes left, LineIndexes right) =>
            !left.Equals(right);

        public static LineIndexes Parse(string src)
        {
            var parts = src.Split(new [] { '|' }, 
                StringSplitOptions.RemoveEmptyEntries);

            return new LineIndexes
            {
                start = int.Parse(parts[0]),
                lettersCount = byte.Parse(parts[1]),
                digitsCount = byte.Parse(parts[2]),

                sortingOffset = parts.Length < 4 
                              ? DefaultSortingOffset 
                              : byte.Parse(parts[3]),

                sortByDigits = parts.Length < 5 
                             ? DefaultSortByDigits
                             : bool.Parse(parts[4])
            };
        }
    }
}
