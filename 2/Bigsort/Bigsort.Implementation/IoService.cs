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
            new BufferingWriter(path, _buffersPool.GetBuffer());
        
        public IWriter OpenBufferingWrite(string path) =>
            new BufferingWriter(path, _buffersPool.GetBuffer());

        public IAsyncWriter OpenAsyncBufferingWrite(string path, ITasksQueue tasksQueue) =>
            new AsyncBufferinWriter(path, _buffersPool, tasksQueue);

        public IWriter OpenBufferingAsyncWrite(string path, ITasksQueue tasksQueue) =>
            new BufferingAsyncWriter(path, _buffersPool, tasksQueue);

        public IWriter OpenSharedWrite(string path, long possition) =>
            new BufferingWriter(path, possition, _buffersPool.GetBuffer());

        public long SizeOfFile(string path) =>
            new FileInfo(path).Length;

        public void CreateFile(string path, long length)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew))
                stream.SetLength(length);
        }

        public IEnumerable<string> EnumerateFilesOf(string directory) =>
            Directory.EnumerateFiles(directory);

        public bool DirectoryExists(string path) =>
            Directory.Exists(path);

        public void CreateDirectory(string path) =>
            Directory.CreateDirectory(path);

        public void DeleteFile(string path) =>
            File.Delete(path);

        private class BufferingWriter
            : IWriter
        {
            private readonly IUsingHandle<byte[]> _buffHandle;
            private readonly byte[] _buff;
            private readonly Stream _stream;
            private int _offset = 0;

            public BufferingWriter(string path, 
                IUsingHandle<byte[]> buffHandle)
            {       
                _stream = File.OpenWrite(path);
                _buffHandle = buffHandle;
                _buff = _buffHandle.Value;
            }

            public BufferingWriter(string path, long possition, 
                IUsingHandle<byte[]> buffHandle)
            {
                _stream = new FileStream(path,
                    FileMode.OpenOrCreate,
                    FileAccess.Read,
                    FileShare.Write)
                {
                    Position = possition
                };

                _buffHandle = buffHandle;
                _buff = _buffHandle.Value;
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

            public void Write(byte x)
            {
                if (_offset == _buff.Length)
                {
                    _stream.Write(_buff, 0, _buff.Length);
                    _offset = 0;
                }

                _buff[_offset++] = x;
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

        private class BufferingAsyncWriter
            : IWriter
        {
            private readonly IBuffersPool _buffersPool;
            private readonly ITasksQueue _tasksQueue;
            private readonly Stream _stream;
            private IUsingHandle<byte[]> _buffHandle;
            private byte[] _buff;
            private int _offset;

            public BufferingAsyncWriter(
                string path,
                IBuffersPool buffersPool, 
                ITasksQueue tasksQueue)
            {
                _buffersPool = buffersPool;
                _tasksQueue = tasksQueue;
                _buffHandle = _buffersPool.GetBuffer();
                _buff = _buffHandle.Value;
                _stream = File.OpenWrite(path);
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

                    var oldBuffHandle = _buffHandle;
                    _buffHandle = _buffersPool.GetBuffer();
                    _buff = _buffHandle.Value;
                    _offset = 0;

                    _tasksQueue.Enqueue(
                        () =>
                        {
                            using (oldBuffHandle)
                                _stream.Write(oldBuffHandle.Value, 0,
                                    oldBuffHandle.Value.Length);
                        });

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
                throw new NotImplementedException();
            }

            public void Flush()
            {
                if (_offset != 0)
                {
                    var oldOffset = _offset;
                    _tasksQueue.Enqueue(() =>
                        _stream.Write(_buff, 0, oldOffset));
                    _offset = 0;
                }
            }

            public void Dispose()
            {
                _buffHandle.Dispose();
                _stream.Dispose();
            }
        }

        private class AsyncBufferinWriter
            : IAsyncWriter
        {
            private readonly object o = new object(); 

            private readonly IBuffersPool _buffersPool;
            private readonly ITasksQueue _tasksQueue;
            private readonly Stream _stream;
            private IUsingHandle<byte[]> _buffHandle;
            private byte[] _buff;
            private int _offset;

            public AsyncBufferinWriter(string path,
                IBuffersPool buffersPool, 
                ITasksQueue tasksQueue)
            {
                _buffersPool = buffersPool;
                _tasksQueue = tasksQueue;
                _buffHandle = _buffersPool.GetBuffer();
                _buff = _buffHandle.Value;
                _stream = File.OpenWrite(path);
            }

            public long Length =>
                _stream.Length;

            public void Write(IUsingHandle<byte[]> arrayHandle, 
                int offset, int count)
            {
                lock (o)
                    if (_offset + count > _buff.Length)
                    {
                        var oldBuffHandle = _buffHandle;
                        _buffHandle = _buffersPool.GetBuffer();
                        _buff = _buffHandle.Value;
                        
                        var lengthToBuffEnd = _buff.Length - _offset;
                        var newBuffOffset = count - lengthToBuffEnd;
                        var oldOffset = _offset;
                        _offset = newBuffOffset;

                        _tasksQueue.Enqueue(() =>
                        {
                            using (arrayHandle)
                            {
                                var sourceBuff = arrayHandle.Value;
                                using (oldBuffHandle)
                                {
                                    var oldBuff = oldBuffHandle.Value;
                                    Array.Copy(sourceBuff, offset,
                                               oldBuff, _offset,
                                               lengthToBuffEnd);
                                    _stream
                                         .Write(oldBuff, 0, oldBuff.Length);
                                }

                                Array.Copy(sourceBuff, offset,
                                           _buff, oldOffset,
                                           count);
                            }
                        });
                    }
                    else
                    {
                        var oldOffset = _offset;
                        _offset += count;
                        _tasksQueue.Enqueue(() =>
                        {
                            using (arrayHandle)
                                Array.Copy(arrayHandle.Value, offset,
                                           _buff, oldOffset,
                                           count);
                        });
                    }
            }

            public void Dispose() =>
                _tasksQueue.Enqueue(() =>
                {
                    _stream.Write(_buff, 0, _offset);
                    _buffHandle.Dispose();
                    _stream.Dispose();
                });
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
