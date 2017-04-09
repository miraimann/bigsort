using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class BuffersReaderMaker
        : IBuffersReaderMaker
    {
        private readonly IBuffersPool _buffersPool;
        private readonly IIoService _ioService;
        private readonly IUsingHandleMaker _usingHandleMaker;

        public BuffersReaderMaker(
            IBuffersPool buffersPool,
            IIoService ioService,
            IUsingHandleMaker usingHandleMaker)
        {
            _buffersPool = buffersPool;
            _ioService = ioService;
            _usingHandleMaker = usingHandleMaker;
        }

        public IBuffersReader Make(
                string path, int buffLength,
                ITasksQueue tasksQueue) =>

            new BuffersReader(
                path, buffLength,
                _buffersPool,
                tasksQueue,
                _ioService,
                _usingHandleMaker);

        private class BuffersReader
            : IBuffersReader
        {
            private const int InitCapacity = 16,
                TemporaryMissingResult = -1;

            private readonly string _path;
            private readonly long _fileLength;
            private readonly int _bufferLength, _readerStep, _capacity;

            private readonly IUsingHandleMaker _usingHandleMaker;
            private readonly IBuffersPool _buffersPool;
            private readonly ITasksQueue _tasksQueue;
            private readonly IIoService _ioService;

            private readonly ConcurrentDictionary<int, Item> _readed;
            private readonly IEnumerator<int> _readingIndex;
            private readonly IUsingHandle<byte[]> _zeroHandle;

            public BuffersReader(
                string path,
                int buffLength,
                IBuffersPool buffersPool,
                ITasksQueue tasksQueue,
                IIoService ioService,
                IUsingHandleMaker usingHandleMaker)
            {
                _usingHandleMaker = usingHandleMaker;
                _buffersPool = buffersPool;
                _tasksQueue = tasksQueue;
                _ioService = ioService;

                _zeroHandle = _usingHandleMaker
                    .Make<byte[]>(null, _ => { });

                _path = path;
                _bufferLength = buffLength;

                _fileLength = _ioService.SizeOfFile(path);
                _capacity = (int) Math.Min(
                    Math.Ceiling((double) _fileLength/_bufferLength),
                    InitCapacity);

                _readed = new ConcurrentDictionary<int, Item>(
                    concurrencyLevel: _tasksQueue.MaxThreadsCount,
                            capacity: _capacity);

                _readerStep = (_capacity - 1) * buffLength;
                _readingIndex = ReadingIndexes.GetEnumerator();
                _readingIndex.MoveNext();
                
                for (int i = 0; i < _capacity; i++)
                    AddItem(i);
            }

            public int ReadNext(out IUsingHandle<byte[]> buffHandle)
            {
                Item x;
                if (_readed.TryRemove(_readingIndex.Current, out x))
                {
                    buffHandle = x.Handle;
                    _readingIndex.MoveNext();
                    return x.Length;
                }

                buffHandle = null;
                return TemporaryMissingResult;
            }

            public void Dispose() =>
                _readingIndex.Dispose();

            private IEnumerable<int> ReadingIndexes
            {
                get
                {
                    while (true)
                        for (int i = 0; i < _capacity; i++)
                            yield return i;
                }
            }

            private void AddItem(int i)
            {
                var pooledBuff = _buffersPool.GetBuffer();
                var reader = _ioService
                    .OpenPositionableRead(_path, i * _bufferLength);

                IUsingHandle<byte[]> handle = null;
                Action readNext = null;

                readNext = delegate
                {
                    var length = reader.Read(pooledBuff.Value, 0, _bufferLength);
                    _readed.TryAdd(i, new Item(length, handle));
                };
                
                handle = _usingHandleMaker.Make(pooledBuff.Value, delegate
                {
                    var possition = reader.Possition + _readerStep;
                    if (possition < _fileLength)
                    {
                        reader.Possition = possition;
                        _tasksQueue.Enqueue(readNext);
                    }
                    else
                    {
                        _readed.TryAdd(i, new Item(0, _zeroHandle));
                        pooledBuff.Dispose();
                        reader.Dispose();
                    }
                });

                _tasksQueue.Enqueue(readNext);
            }
        }

        private struct Item
        {
            public Item(int length, IUsingHandle<byte[]> handle)
            {
                Length = length;
                Handle = handle;
            }

            public readonly int Length;
            public readonly IUsingHandle<byte[]> Handle;
        }
    }
}
