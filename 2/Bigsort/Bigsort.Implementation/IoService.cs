using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class IoService
        : IIoService
    {
        private readonly IConfig _config;
        private readonly IPool<byte[]> _buffersPool;

        public IoService(
            IPoolMaker poolMaker,
            IConfig config)
        {
            _config = config;
            _buffersPool = poolMaker.Make(
                create: () => new byte[_config.BufferSize]);
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

        public IBytesMatrix ReadToBytesMatrix(string path) =>
            new BuffersSet(path, _buffersPool, _config);

        public IWriter OpenWrite(string path) =>
            new Writer(path, _buffersPool.Get());

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

        private class BuffersSet
            : IFixedSizeList<byte> 
            , IBytesMatrix
        {
            private readonly IPooled<byte[]>[] _buffHandles;
            
            public BuffersSet(
                string path, 
                IPool<byte[]> buffersPool, 
                IConfig config)
            {
                using (var stream = File.OpenRead(path))
                {
                    Count = (int)stream.Length;
                    RowLength = config.BufferSize;
                    RowsCount = (Count / RowLength) 
                              + (Count % RowLength == 0 ? 0 : 1);
                    
                    _buffHandles = new IPooled<byte[]>[RowsCount];
                    Content = new byte[RowsCount][];

                    for (int i = 0; i < RowsCount; i++)
                    {
                        _buffHandles[i] = buffersPool.Get();
                        Content[i] = _buffHandles[i].Value;
                    }   
                }
            }

            public byte[][] Content { get; }
            public int RowsCount { get; }
            public int RowLength { get; }
            public int Count { get; }

            public byte this[int i]
            {
                get { return Content[i/RowsCount][i%RowsCount]; }
                set { Content[i/RowsCount][i%RowsCount] = value; }
            }
            
            public IFixedSizeList<byte> AdaptInLine() =>
                this;

            public IReadOnlyList<byte> AsReadOnlyList() =>
                this;
            
            public IEnumerator<byte> GetEnumerator() =>
                Content.Select(Enumerable.AsEnumerable)
                       .Aggregate(Enumerable.Concat)
                       .Take(Count)
                       .GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() =>
                GetEnumerator();

            public void Dispose()
            {
                foreach (var handle in _buffHandles)
                    handle.Dispose();
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
