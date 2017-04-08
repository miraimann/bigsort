using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class BuffersReaderMaker
        : IBuffersReaderMaker
    {
        private const int Empty = -1, Reserved = -2;
        private readonly IBuffersPool _buffersPool;
        private readonly IIoService _ioService;

        public BuffersReaderMaker(
            IBuffersPool buffersPool, 
            IIoService ioService)
        {
            _buffersPool = buffersPool;
            _ioService = ioService;
        }

        public IBuffersReader Make(string path, int buffLength,
                ITasksQueue tasksQueue) =>

            new BuffersReader(path, buffLength,
                _buffersPool, tasksQueue, _ioService);

        private class BuffersReader
            : IBuffersReader
        {
            private const int CacheSize = 10;

            private readonly IBuffersPool _buffersPool;
            private readonly ITasksQueue _tasksQueue;
            private readonly IIoService _ioService;
            private readonly int _buffLength, _readerStep;
            private readonly IUsingHandle<byte[]>[] _cache;
            private readonly int[] _buffersLengthes;
            private readonly IEnumerator<IPositionableReader> _reader;
            private readonly IEnumerator<int> _r, _w;
            
            public BuffersReader(
                string path,
                int buffLength,
                IBuffersPool buffersPool, 
                ITasksQueue tasksQueue, 
                IIoService ioService)
            {
                _buffersPool = buffersPool;
                _tasksQueue = tasksQueue;
                _ioService = ioService;
                _buffLength = buffLength;

                var readersCount = CacheSize; // _tasksQueue.MaxThreadsCount;
                _readerStep = _buffLength * (readersCount - 1);

                 _cache = new IUsingHandle<byte[]>[CacheSize];
                _buffersLengthes = Enumerable
                    .Repeat(Empty, CacheSize)
                    .ToArray();

                _r = CacheIndexes.GetEnumerator();
                _w = CacheIndexes.GetEnumerator();
                _reader = CreateReaders(path)
                    .GetEnumerator();

                _reader.MoveNext();
                _w.MoveNext();
                _r.MoveNext();

                UpdateCache();
            }

            public int ReadNext(out IUsingHandle<byte[]> buffHandle)
            {
                var r = _r.Current;
                if (_buffersLengthes[r] <= Empty)
                {
                    if (_buffersLengthes[r] != Reserved)
                        UpdateCache();

                    while (_buffersLengthes[r] <= Empty)
                        Thread.Sleep(1); // LOOK
                }

                buffHandle = _cache[r];
                var length = _buffersLengthes[r];
                _buffersLengthes[r] = Empty;
                _r.MoveNext();

                return length;   
            }

            private void UpdateCache()
            {
                while (_buffersLengthes[_w.Current] == Empty)
                {
                    var reader = _reader.Current;
                    var w = _w.Current;

                    _buffersLengthes[w] = Reserved;
                    _tasksQueue.Enqueue(() =>
                    {
                        _cache[w] = _buffersPool.GetBuffer();
                        _buffersLengthes[w] = reader
                            .Read(_cache[w].Value, 0, _buffLength);
                        reader.Possition += _readerStep;
                    });
                    
                    _reader.MoveNext();
                    _w.MoveNext();
                }
            }
                
            private IEnumerable<IPositionableReader> CreateReaders(string path)
            {
                var fileLength = _ioService.SizeOfFile(path);
                var readers = Enumerable
                    .Range(0, CacheSize) // _tasksQueue.MaxThreadsCount) // LOOK
                    .Select(i => i * _buffLength)
                     .Where(i => i < fileLength)
                    .Select(i => _ioService.OpenPositionableRead(path, i))
                    .ToArray();
                
                while (true)
                    foreach (var reader in readers)
                        yield return reader;
            }

            private IEnumerable<int> CacheIndexes
            {
                get
                {
                    while (true)
                        for (int i = 0; i < CacheSize; i++)
                            yield return i;
                }
            }

            public void Dispose()
            {
                for (int i = 0; i < CacheSize /* _tasksQueue.MaxThreadsCount */; i++)
                {
                    _reader.MoveNext();
                    _reader.Current.Dispose();
                }

                _reader.Dispose();
                _w.Dispose();
                _r.Dispose();
            }
        }
    }
}
