﻿using System;
using System.IO;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class IoService
        : IIoService
    {
        private readonly IBuffersPool _buffersPool;

        public IoService(IBuffersPool buffersPool)
        {
            _buffersPool = buffersPool;
        }

        public IFileReader OpenRead(string path, long position = 0) =>
            new Reader(path, position);

        public IFileWriter OpenWrite(string path,
                long position = 0,
                bool buffering = false) =>

            buffering
                ? new BufferingWriter(path, position, _buffersPool.Get())
                    as IFileWriter
                : new Writer(path, position);

        public long SizeOfFile(string path) =>
            new FileInfo(path).Length;

        public void CreateFile(string path, long length)
        {
            using (var stream = new FileStream(path, FileMode.OpenOrCreate))
                stream.SetLength(length);
        }

        public void DeleteFile(string path) =>
            File.Delete(path);

        private struct Writer
            : IFileWriter
        {
            private readonly Stream _stream;

            public Writer(string path, long position)
            {
                _stream = new FileStream(path,
                    FileMode.OpenOrCreate,
                    FileAccess.Write,
                    FileShare.Write)
                {
                    Position = position
                };
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

        private struct BufferingWriter
            : IFileWriter
        {
            private readonly Handle<byte[]> _buffHandle;
            private readonly byte[] _buff;
            private readonly Stream _stream;
            private int _offset;

            public BufferingWriter(
                string path, 
                long possition, 
                Handle<byte[]> buffHandle)
            {
                _stream = new FileStream(path,
                    FileMode.OpenOrCreate,
                    FileAccess.Write,
                    FileShare.Write)
                {
                    Position = possition
                };

                _buffHandle = buffHandle;
                _buff = _buffHandle.Value;
                _offset = 0;
            }

            public long Position
            {
                get { return _stream.Position; }
                set { _stream.Position = value; }
            }

            public long Length =>
                _stream.Length;

            public void Write(byte[] array, int offset, int count)
            {
                if (_offset + count > _buff.Length)
                {
                    var countToBuffEnd = _buff.Length - _offset;
                    Array.Copy(array, offset,
                        _buff, _offset,
                        countToBuffEnd);

                    _stream.Write(_buff, 0, _buff.Length);
                    _offset = 0;

                    Write(array,
                          offset + countToBuffEnd,
                          count - countToBuffEnd);
                    return;
                }

                Array.Copy(array, offset,
                           _buff, _offset,
                           count);

                _offset += count;
            }

            public void Flush()
            {
                if (_offset != 0)
                {
                    _stream.Write(_buff, 0, _offset);
                    _offset = 0;
                }
            }

            public void Dispose()
            {
                Flush();

                _buffHandle.Dispose();
                _stream.Dispose();
            }
        }

        private class Reader
            : IFileReader
        {
            private readonly Stream _stream;

            public Reader(string path, long position = 0)
            {
                _stream = new FileStream(path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read)
                {
                    Position = position
                };
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
