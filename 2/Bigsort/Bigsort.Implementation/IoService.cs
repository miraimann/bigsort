using System;
using System.Collections.Generic;
using System.IO;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class IoService
        : IIoService
    {
        private readonly IBuffersPool _buffersPool; 
        public IoService(IBuffersPool buffersPool)
        {
            _buffersPool = buffersPool;
        }

        public string TempDirectory { get; } =
            Path.GetTempPath();

        public string CurrentDirectory
        {
            get { return Environment.CurrentDirectory; }
            set { Environment.CurrentDirectory = value; }
        }

        public IReader OpenRead(string path) =>
            new Reader(path);

        public IWriter OpenWrite(string path) =>
            new Writer(path, _buffersPool.Get());

        public IEnumerable<string> EnumerateFilesOf(string directory) =>
            Directory.EnumerateFiles(directory);

        public void CreateDirectory(string path) =>
            Directory.CreateDirectory(path);

        public void DeleteFile(string path) =>
            File.Delete(path);

        private class Writer
            : IWriter
        {
            private readonly IPooled<byte[]> _buffHandle;
            private readonly byte[] _buff;
            private readonly Stream _stream;
            private int _offset = 0;

            public Writer(string path, IPooled<byte[]> buffHandler)
            {       
                _stream = File.OpenWrite(path);
                _buffHandle = buffHandler;
                _buff = _buffHandle.Value;
            }

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

            public void Write(byte x)
            {
                if (_offset == _buff.Length)
                {
                    _stream.Write(_buff, 0, _buff.Length);
                    _offset = 0;
                }

                _buff[_offset++] = x;
            }

            public void Dispose()
            {
                if (_offset != 0)
                    _stream.Write(_buff, 0, _offset);

                _buffHandle.Dispose();
                _stream.Dispose();
            }
        }

        private class Reader
            : IReader
        {
            private readonly Stream _stream;

            public Reader(string path)
            {
                _stream = File.OpenRead(path);
            }

            public int Read(byte[] buff, int offset, int count) =>
                _stream.Read(buff, offset, count);

            public void Dispose() =>
                _stream.Dispose();
        }
    }
}
