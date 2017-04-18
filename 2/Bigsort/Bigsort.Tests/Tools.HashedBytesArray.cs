using System;
using System.Linq;

namespace Bigsort.Tests
{
    public static partial class Tools
    {
        public static HashedBytesArray Hash(byte[] array) =>
            new HashedBytesArray(array);

        public class HashedBytesArray
            : IEquatable<HashedBytesArray>
        {
            public HashedBytesArray(byte[] value)
            {
                Value = value;
            }

            public byte[] Value { get; }

            public bool Equals(HashedBytesArray other) =>
                Value.Length == other.Value.Length
                && Enumerable.Zip(Value, other.Value, (x, y) => x == y)
                             .All(o => o);

            public override bool Equals(object obj) =>
                Equals((HashedBytesArray)obj);

            public override int GetHashCode() =>
                Value.Length >= sizeof(int)
                    ? BitConverter.ToInt32(Value, 0)
                    : Value.Reverse() // for little endian
                           .Select((x, i) => x * (int)Math.Pow(byte.MaxValue, i))
                           .Sum();
        }
    }
}
