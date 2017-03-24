using System;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class BytesConvertersFactory
        : IBytesConvertersFactory
    {
        public IBytesConverter<int> CreateForInt() =>
            new IntConverter();

        public IBytesConverter<long> CreateForLong() =>
            new LongConverter();

        private class IntConverter
            : IBytesConverter<int>
        {
            public int FromBytes(byte[] buff, int offset) =>
                BitConverter.ToInt32(buff, offset);

            public byte[] ToBytes(int value) =>
                BitConverter.GetBytes(value);
        }

        private class LongConverter
            : IBytesConverter<long>
        {
            public long FromBytes(byte[] buff, int offset) =>
                BitConverter.ToInt32(buff, offset);

            public byte[] ToBytes(long value) =>
                BitConverter.GetBytes(value);
        }
    }
}
