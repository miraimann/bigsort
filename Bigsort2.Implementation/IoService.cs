using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bigsort2.Contracts;

namespace Bigsort2.Implementation
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

        public void SetCurrentDirectory(string path) =>
            Environment.CurrentDirectory = path;

        public IReader OpenRead(string path)
        {
            throw new NotImplementedException();
        }

        public IWriter OpenWrite(string path) =>
            new Writer(path, _buffersPool.Get());

        public void DeleteFile(string path)
        {
            throw new NotImplementedException();
        }

        private class Writer
            : IWriter
        {
            private readonly IPooled<byte[]> _buffHandler;
            private readonly byte[] _buff;
            private readonly Stream _stream;
            private int _offset = 0;

            public Writer(string path, IPooled<byte[]> buffHandler)
            {       
                _stream = File.OpenWrite(path);
                _buffHandler = buffHandler;
                _buff = _buffHandler.Value;
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

                _buffHandler.Free();
                _stream.Dispose();
            }
        }

        private class Reader
            : IReader
        {
            private readonly IPooled<byte[]> _buffHandler1, _buffHandler2;
            private readonly byte[] buff1, buff2;
            private readonly Stream _stream;

            public Reader(string path, 
                IPooled<byte[]> buffHandler1,
                IPooled<byte[]> buffHandler2)
            {
                _buffHandler1 = buffHandler1;
                _buffHandler2 = buffHandler2;
                buff1 = buffHandler1.Value;
                buff2 = buffHandler1.Value;

                _stream = File.OpenRead(path);
            }

            public long Position { get; set; }
            
            public byte NextByte()
            {
                throw new NotImplementedException();
            }

            public ushort NextUInt16()
            {
                throw new NotImplementedException();
            }

            public ulong NextUInt64()
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }
    }
}
