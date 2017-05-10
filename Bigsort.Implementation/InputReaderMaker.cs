using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class InputReaderMaker
        : IInputReaderMaker
    {
        private readonly string _inputFilePath;
        private readonly IIoService _ioService;
        private readonly ITasksQueue _tasksQueue;
        private readonly IBuffersPool _buffersPool;
        private readonly IConfig _config;

        public InputReaderMaker(
            string inputFilePath,
            IIoService ioService,
            ITasksQueue tasksQueue, 
            IBuffersPool buffersPool,
            IConfig config)
        {
            _inputFilePath = inputFilePath;
            _ioService = ioService;
            _tasksQueue = tasksQueue;
            _buffersPool = buffersPool;
            _config = config;
        }

        public IInputReader Make(long fileLength) =>
            Make(0, fileLength);

        public IInputReader Make(long fileOffset, long readingLength) =>
            new InputReader(
                _inputFilePath, 
                fileOffset, 
                readingLength, 
                _buffersPool,
                _tasksQueue,
                _ioService,
                _config);
        
        private class InputReader
            : IInputReader
        {
            private const int InitCapacity = 16;

            private readonly long _readingOut;
            private readonly int _capacity;
            
            private readonly IPool<byte[]> _buffersPool;
            private readonly ITasksQueue _tasksQueue;

            private readonly Item _firstBufferItem;
            private readonly ConcurrentDictionary<int, Item> _readed;
            private readonly IEnumerator<int> _readingIndex;
            private readonly Action[] _readNext;

            public InputReader(
                string inputFilePath,
                long readingOffset,
                long readingLength,
                IPool<byte[]> buffersPool,
                ITasksQueue tasksQueue,
                IIoService ioService,
                IConfig config)
            {
                _buffersPool = buffersPool;
                _tasksQueue = tasksQueue;
                
                _readingOut = readingOffset + readingLength;

                // -1 - for set "Buffer End" Symbol to last cell of buffer without data lose 
                var readingBufferLength = config.UsingBufferLength - 1;

                using (var reader = ioService.OpenRead(inputFilePath))
                {
                    var firstBufferHandle = _buffersPool.Get();
                    var length = (int) Math.Min(
                        readingBufferLength - Consts.EndLineBytesCount, 
                        readingLength);

                    reader.Position = readingOffset;
                    length = reader.Read(firstBufferHandle.Value,
                                         Consts.EndLineBytesCount,
                                         length);

                    _firstBufferItem = new Item(
                        length + Consts.EndLineBytesCount, 
                        firstBufferHandle);

                    readingOffset += length;
                    readingLength -= length;
                }

                _capacity = (int)Math.Min(
                    Math.Ceiling((double)readingLength / readingBufferLength),
                    InitCapacity);

                _readed = new ConcurrentDictionary<int, Item>(
                    concurrencyLevel: config.MaxRunningTasksCount,
                            capacity: Math.Max(1, _capacity));

                if (_capacity == 0)
                {
                    _readed.TryAdd(0, new Item(0, Handle<byte[]>.Zero));
                    _readNext = new Action[] { () => _readed.TryAdd(0, new Item(0, Handle<byte[]>.Zero)) };
                    _readingIndex = Enumerable.Repeat(0, int.MaxValue).GetEnumerator();
                    return;
                }

                var readerStep = (_capacity - 1) * readingBufferLength;
                _readingIndex = ReadingIndexes.GetEnumerator();
                _readingIndex.MoveNext();

                _readNext = new Action[_capacity];
                for (int i = 0; i < _capacity; i++)
                {
                    var reader = ioService.OpenRead(inputFilePath, 
                        position: readingOffset + i * readingBufferLength);

                    int buffIndex = i;
                    Action read = delegate
                    {
                        var pooledBuff = _buffersPool.Get();
                        var length = (int) Math.Min(_readingOut - reader.Position, readingBufferLength);

                        length = reader.Read(pooledBuff.Value, 0, length);
                        _readed.TryAdd(buffIndex, new Item(length, pooledBuff));
                    };
                
                    _readNext[i] = delegate
                    {
                        read();
                        _readNext[buffIndex] = delegate
                        {
                            var position = reader.Position + readerStep;
                            if (position < _readingOut)
                            {
                                reader.Position = position;
                                read();
                            }
                            else
                            {
                                _readed.TryAdd(buffIndex, new Item(0, Handle<byte[]>.Zero));
                                reader.Dispose();
                            }
                        };
                    };
                }

                for (int i = 0; i < _readNext.Length; i++)
                    _tasksQueue.Enqueue(_readNext[i]);
            }

            public int GetFirstBuffer(out Handle<byte[]> buffHandle)
            {
                buffHandle = _firstBufferItem.Handle;
                return _firstBufferItem.Length;
            }

            public int TryGetNextBuffer(out Handle<byte[]> buffHandle)
            {
                Item x;
                if (_readed.TryRemove(_readingIndex.Current, out x))
                {
                    if (x.Length != 0)
                        _tasksQueue.Enqueue(_readNext[_readingIndex.Current]);

                    _readingIndex.MoveNext();
                    buffHandle = x.Handle;
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
        }

        private struct Item
        {
            public Item(int length, Handle<byte[]> handle)
            {
                Length = length;
                Handle = handle;
            }

            public readonly int Length;
            public readonly Handle<byte[]> Handle;
        }
    }
}
