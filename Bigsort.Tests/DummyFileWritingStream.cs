using System.Collections.Generic;
using System.IO;
using Bigsort.Contracts;

namespace Bigsort.Tests
{
    public class DummyFileWritingStream
                : IWritingStream
    {
        private readonly Dictionary<string, byte[]> _streamsContent;
        private readonly string _path;
        private readonly MemoryStream _memoryStream;

        public DummyFileWritingStream(
            Dictionary<string, byte[]> streamsContent, 
            string path)
        {
            _memoryStream = new MemoryStream();
            _streamsContent = streamsContent;
            _path = path;
        }

        public long Position
        {
            get { return _memoryStream.Position; }
            set { _memoryStream.Position = value; }
        }

        public void Dispose()
        {
            _streamsContent.Add(_path, _memoryStream.ToArray());
            _memoryStream.Dispose();
        }

        public void Write(byte[] array, int offset, int count) =>
            _memoryStream.Write(array, offset, count);

        public void WriteByte(byte x) =>
            _memoryStream.WriteByte(x);
    }
}
