using System.IO;
using Bigsort.Contracts;

namespace Bigsort.Tests
{
    public static partial class Tools
    {
        public class MemoryReader
            : IFileReader
        {
            private readonly MemoryStream _stream;
            public MemoryReader(MemoryStream stream)
            {
                _stream = stream;
            }

            public long Position
            {
                get { return _stream.Position; }
                set { _stream.Position = value; }
            }

            public long Length =>
                _stream.Length;

            public int Read(byte[] buff, int offset, int count) =>
                _stream.Read(buff, offset, count);

            public int ReadByte() =>
                _stream.ReadByte();

            public void Dispose() =>
                _stream.Dispose();
        }
    }
}
