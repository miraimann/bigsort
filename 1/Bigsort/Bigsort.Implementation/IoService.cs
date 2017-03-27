using Bigsort.Contracts;
using System.Collections.Generic;
using System.IO;

namespace Bigsort.Implementation
{
    internal class IoService
        : IIoService
    {
        private readonly IConfig _config;

        public IoService(IConfig config)
        {
            TempDirectory = Path.GetTempPath();
            _config = config;
        }

        public string TempDirectory { get; }

        public IEnumerable<byte> EnumeratesBytesOf(string path)
        {
            var buff = new byte[_config.BytesEnumeratingBufferSize];
            using (var input = OpenRead(path))
            {
                int count;
                while ((count = input.Read(buff, 0, buff.Length)) != 0)
                    for (int i = 0; i < count; i++)
                        yield return buff[i];
            }
        }

        public IReadingStream OpenRead(string path) =>
            new Stream(File.OpenRead(path));

        public IWritingStream OpenWrite(string path) =>
            new Stream(File.OpenWrite(path));

        public IStream CreateInMemory() =>
            new Stream(new MemoryStream());

        public IStream Adapt(MemoryStream stream) =>
            new Stream(stream);

        public ITextWriter Adapt(System.IO.TextWriter writer) =>
            new TextWriter(writer);

        public void DeleteFile(string path) =>
            File.Delete(path);

        private class Stream
            : IStream
        {
            private readonly System.IO.Stream _implementation;
            public Stream(System.IO.Stream implementation)
            {
                _implementation = implementation;
            }

            public long Length =>
                _implementation.Length;

            public long Position
            {
                get { return _implementation.Position; }
                set { _implementation.Position = value; }
            }

            public void Write(byte[] array, int offset, int count) =>
                _implementation.Write(array, offset, count);

            public void WriteByte(byte x) =>
                _implementation.WriteByte(x);

            public int Read(byte[] array, int offset, int count) =>
                _implementation.Read(array, offset, count);

            public int ReadByte() =>
                _implementation.ReadByte();

            public void Dispose() =>
                _implementation.Dispose();
        }

        public class TextWriter
            : ITextWriter
        {
            private readonly System.IO.TextWriter _writer;

            public TextWriter(System.IO.TextWriter writer)
            {
                _writer = writer;
            }

            public void WriteLine(string format, params object[] args) =>
                _writer.WriteLine(format, args);

            public void WriteLine() =>
                _writer.WriteLine();

            public void Dispose() =>
                _writer.Dispose();
        }
    }
}
