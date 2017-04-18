using System.IO;
using Bigsort.Contracts;

namespace Bigsort.Tests
{
    public static partial class Tools
    {
        public class MemoryWriter
            : IFileWriter
        {
            private readonly MemoryStream _stream;
            public MemoryWriter(MemoryStream stream)
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

            public void Write(byte[] array, int offset, int count) =>
                _stream.Write(array, offset, count);

            public void Flush() =>
                _stream.Flush();

            public void Dispose() =>
                _stream.Dispose();
        }
    }
}
