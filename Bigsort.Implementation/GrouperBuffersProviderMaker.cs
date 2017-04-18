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
        private readonly ITasksQueue _tasksQueue;
        private readonly IConfig _config;

        public GrouperBuffersProviderMaker(
            IBuffersPool buffersPool,
            IIoService ioService,
            IUsingHandleMaker usingHandleMaker, 
            ITasksQueue tasksQueue, 
            IConfig config)
        {
            _buffersPool = buffersPool;
            _ioService = ioService;
            _usingHandleMaker = usingHandleMaker;
            _tasksQueue = tasksQueue;
            _config = config;
        }
        
        public IGrouperBuffersProvider Make(string path, int buffLength) =>
            Make(path, buffLength, 0, _ioService.SizeOfFile(path));

        public IGrouperBuffersProvider Make(string path, int buffLength,
                long fileOffset, long readingLength) =>

            new BuffersProvider(path, buffLength, fileOffset, readingLength,
                _buffersPool,
                _tasksQueue,
                _ioService,
                _usingHandleMaker,
                _config);
        
        private class BuffersProvider
            : IGrouperBuffersProvider
        {
            private const int InitCapacity = 16; // TODO: move to config
            
            private readonly long _readingOut;
            private readonly int _bufferLength, _capacity;
            
            private readonly IBuffersPool _buffersPool;
            private readonly ITasksQueue _tasksQueue;

            private readonly ConcurrentDictionary<int, Item> _readed;
            private readonly IEnumerator<int> _readingIndex;
            private readonly IUsingHandle<byte[]> _zeroHandle;
            private readonly Action[] _readNext;

            public BuffersProvider(
                string path,
                int buffLength,
                long readingOffset,
                long readingLength,
                IBuffersPool buffersPool,
                ITasksQueue tasksQueue,
                IIoService ioService,
                IUsingHandleMaker usingHandleMaker,
                IConfig config)
            {
                _buffersPool = buffersPool;
                _tasksQueue = tasksQueue;
                _bufferLength = buffLength;

                _zeroHandle = usingHandleMaker.Make<byte[]>(null, _ => { });
                _readingOut = readingOffset + readingLength;
                _capacity = (int) Math.Min(
                    Math.Ceiling((double)readingLength / _bufferLength),
                    InitCapacity);

                _readed = new ConcurrentDictionary<int, Item>(
                    concurrencyLevel: config.MaxRunningTasksCount,
                            capacity: _capacity);

                var readerStep = (_capacity - 1) * buffLength;
                _readingIndex = ReadingIndexes.GetEnumerator();
                _readingIndex.MoveNext();

                _readNext = new Action[_capacity];
                for (int i = 0; i < _capacity; i++)
                {
                    var reader = ioService.OpenRead(path, readingOffset + i*_bufferLength);

                    int buffIndex = i;
                    Action read = delegate
                    {
                        var pooledBuff = _buffersPool.GetBuffer();
                        var length = (int) Math.Min(_readingOut - reader.Position, _bufferLength);

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

                foreach (Action x in _readNext)
                    _tasksQueue.Enqueue(x);
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
