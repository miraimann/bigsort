using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class InputReaderMaker
        : IInputReaderMaker
    {
        private readonly IIoServiceMaker _ioServiceMaker;
        private readonly IUsingHandleMaker _usingHandleMaker;
        private readonly ITasksQueue _tasksQueue;
        private readonly IConfig _config;

        public InputReaderMaker(
            IIoServiceMaker ioServiceMaker,
            IUsingHandleMaker usingHandleMaker, 
            ITasksQueue tasksQueue, 
            IConfig config)
        {
            _ioServiceMaker = ioServiceMaker;
            _usingHandleMaker = usingHandleMaker;
            _tasksQueue = tasksQueue;
            _config = config;
        }

        public IInputReader Make(
                string inputPath,
                long fileLength,
                IPool<byte[]> buffersPool) =>

            Make(inputPath, 0, fileLength, buffersPool);

        public IInputReader Make(
                string inputPath, 
                long fileOffset, 
                long readingLength, 
                IPool<byte[]> buffersPool) =>

            new BuffersProvider(
                inputPath, 
                fileOffset, 
                readingLength, 
                buffersPool,
                _tasksQueue,
                _ioServiceMaker.Make(buffersPool),
                _usingHandleMaker,
                _config);
        
        private class BuffersProvider
            : IInputReader
        {
            private const int InitCapacity = 16;

            private readonly long _readingOut;
            private readonly int _capacity;
            
            private readonly IPool<byte[]> _buffersPool;
            private readonly ITasksQueue _tasksQueue;

            private readonly ConcurrentDictionary<int, Item> _readed;
            private readonly IEnumerator<int> _readingIndex;
            private readonly IUsingHandle<byte[]> _zeroHandle;
            private readonly Action[] _readNext;

            public BuffersProvider(
                string path,
                long readingOffset,
                long readingLength,
                IPool<byte[]> buffersPool,
                ITasksQueue tasksQueue,
                IIoService ioService,
                IUsingHandleMaker usingHandleMaker,
                IConfig config)
            {
                _buffersPool = buffersPool;
                _tasksQueue = tasksQueue;

                _zeroHandle = usingHandleMaker.Make<byte[]>(null, _ => { });
                _readingOut = readingOffset + readingLength;

                // -1 - for set "Buffer End" Symbol to last cell of buffer without data lose 
                var readingBufferLength = config.UsingBufferLength - 1;

                _capacity = (int) Math.Min(
                    Math.Ceiling((double) readingLength / readingBufferLength),
                    InitCapacity);

                _readed = new ConcurrentDictionary<int, Item>(
                    concurrencyLevel: config.MaxRunningTasksCount,
                            capacity: _capacity);

                var readerStep = (_capacity - 1) * readingBufferLength;
                _readingIndex = ReadingIndexes.GetEnumerator();
                _readingIndex.MoveNext();

                _readNext = new Action[_capacity];
                for (int i = 0; i < _capacity; i++)
                {
                    var reader = ioService.OpenRead(path, readingOffset + i * readingBufferLength);

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
                                _readed.TryAdd(buffIndex, new Item(0, _zeroHandle));
                                reader.Dispose();
                            }
                        };
                    };
                }

                for (int i = 0; i < _readNext.Length; i++)
                    _tasksQueue.Enqueue(_readNext[i]);
            }

            public int TryGetNextBuffer(out IUsingHandle<byte[]> buffHandle)
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
