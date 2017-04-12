using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GrouperBuffersProviderMaker
        : IGrouperBuffersProviderMaker
    {
        private readonly IBuffersPool _buffersPool;
        private readonly IIoService _ioService;
        private readonly IUsingHandleMaker _usingHandleMaker;
        private readonly IGrouperTasksQueue _grouperTasksQueue;

        public GrouperBuffersProviderMaker(
            IBuffersPool buffersPool,
            IIoService ioService,
            IUsingHandleMaker usingHandleMaker, 
            IGrouperTasksQueue grouperTasksQueue)
        {
            _buffersPool = buffersPool;
            _ioService = ioService;
            _usingHandleMaker = usingHandleMaker;
            _grouperTasksQueue = grouperTasksQueue;
        }
        
        public IGrouperBuffersProvider Make(string path, int buffLength) =>
            Make(path, buffLength, 0, _ioService.SizeOfFile(path));

        public IGrouperBuffersProvider Make(string path, int buffLength,
                long fileOffset, long readingLength) =>

            new BuffersProvider(path, buffLength, fileOffset, readingLength,
                _buffersPool,
                _grouperTasksQueue.AsLowQueue(),
                _ioService,
                _usingHandleMaker);
        
        private class BuffersProvider
            : IGrouperBuffersProvider
        {
            private const int InitCapacity = 16; // TODO: move to config

            private readonly string _path;
            private readonly long _readingOut, _readingOffset;
            private readonly int _bufferLength, _readerStep, _capacity;

            private readonly IUsingHandleMaker _usingHandleMaker;
            private readonly IBuffersPool _buffersPool;
            private readonly ITasksQueue _tasksQueue;
            private readonly IIoService _ioService;

            private readonly ConcurrentDictionary<int, Item> _readed;
            private readonly IEnumerator<int> _readingIndex;
            private readonly IUsingHandle<byte[]> _zeroHandle;

            public BuffersProvider(
                string path,
                int buffLength,
                long readingOffset,
                long readingLength,
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

                _readingOffset = readingOffset;
                _readingOut = readingOffset + readingLength;
                _capacity = (int) Math.Min(
                    Math.Ceiling((double)readingLength / _bufferLength),
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

            public int TryGetNextBuffer(out IUsingHandle<byte[]> buffHandle)
            {
                Item x;
                if (_readed.TryRemove(_readingIndex.Current, out x))
                {
                    buffHandle = x.Handle;
                    _readingIndex.MoveNext();
                    return x.Length;
                }

                buffHandle = null;
                return Consts.TemporaryMissingResult;
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
                    .OpenPositionableRead(_path, _readingOffset + i * _bufferLength);

                IUsingHandle<byte[]> handle = null;
                Action readNext = delegate
                {
                    var length = (int) Math.Min(_readingOut - reader.Position, _bufferLength);
                    length = reader.Read(pooledBuff.Value, 0, length);
                    _readed.TryAdd(i, new Item(length, handle));
                };
                
                handle = _usingHandleMaker.Make(pooledBuff.Value, delegate
                {
                    var position = reader.Position + _readerStep;
                    if (position < _readingOut)
                    {
                        reader.Position = position;
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
