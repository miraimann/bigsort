using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GroupsLinesWriterMaker
        : IGroupsLinesWriterMaker
    {
        private readonly IIoService _ioService;
        private readonly IBuffersPool _buffersPool;
        private readonly ITasksQueue _tasksQueue;
        private readonly IConfig _config;

        public GroupsLinesWriterMaker(
            IIoService ioService, 
            IBuffersPool buffersPool, 
            IGrouperTasksQueue tasksQueue, 
            IConfig config)
        {
            _ioService = ioService;
            _buffersPool = buffersPool;
            _config = config;
            _tasksQueue = tasksQueue.AsLowQueue();
        }

        public IGroupsLinesWriter Make(string path, long fileOffset = 0) =>
            new LinesWriter(path, fileOffset,
                _buffersPool,
                _tasksQueue,
                _ioService,
                _config);

        private class LinesWriter
            : IGroupsLinesWriter
        {
            private const int InitWritersCount = 16; // TODO: Move to config

            private readonly IIoService _ioService;
            private readonly IBuffersPool _buffersPool;
            private readonly ITasksQueue _tasksQueue;

            private readonly ConcurrentBag<IWriter> _writers;
            private readonly Item[] _storage;
            private readonly int _bufferLength;
            private readonly string _path;
            private readonly long _fileOffset;
            private long _writedBuffersCount;

            public LinesWriter(string path, long fileOffset,
                IBuffersPool buffersPool, 
                ITasksQueue tasksQueue, 
                IIoService ioService,
                IConfig config)
            {
                _path = path;
                _buffersPool = buffersPool;
                _tasksQueue = tasksQueue;
                _ioService = ioService;
                _fileOffset = fileOffset;
                _bufferLength = config.BufferSize;

                _storage = new Item[Consts.MaxGroupsCount];
                _writers = new ConcurrentBag<IWriter>(
                    Enumerable.Range(0, InitWritersCount)
                              .Select(_ => _ioService.OpenWrite(path)));
            }

            public void AddLine(ushort groupId, 
                byte[] buff, int offset, int length)
            {
                var group = SaveData(GetGroup(groupId), buff, offset, length);
                ++group.LinesCount;
                _storage[groupId] = group;
            }

            public void AddBrokenLine(ushort groupId,
                byte[] leftBuff, int leftOffset, int leftLength,
                byte[] rightBuff, int rightOffset, int rightLength)
            {
                var group = GetGroup(groupId);
                group = SaveData(group, leftBuff, leftOffset, leftLength);
                group = SaveData(group, rightBuff, rightOffset, rightLength);

                ++group.LinesCount;
                _storage[groupId] = group;
            }

            public void FlushAndDispose(ManualResetEvent done)
            {
                long counter = 0;
                Item acc = default(Item);
                int i = 0;
                
                while (Item.IsZero(acc = _storage[i++])) ;
                for (Item group = _storage[i]; 
                     i < Consts.MaxGroupsCount; 
                     group = _storage[++i])
                {
                    if (Item.IsZero(group)) continue;

                    SaveData(acc, group.BufferHandle.Value, 0, group.BufferOffset,
                       bufferEnquedToWrite: () => Interlocked.Increment(ref counter),
                             bufferWritten: () => Interlocked.Decrement(ref counter));
                    group.BufferHandle.Dispose();
                }

                if (acc.BufferOffset != 0)
                {
                    Interlocked.Increment(ref counter);
                    _tasksQueue.Enqueue(delegate
                    {
                        var writer = GetWriter();
                        writer.Write(acc.BufferHandle.Value,
                                     0, acc.BufferOffset);

                        Interlocked.Decrement(ref counter);
                        acc.BufferHandle.Dispose();
                        writer.Dispose();
                    });
                }

                _tasksQueue.Enqueue(delegate
                {
                    while (Interlocked.Read(ref counter) != 0)
                        Thread.Sleep(1);

                    foreach (var writer in _writers)
                    {
                        Interlocked.Increment(ref counter);
                        _tasksQueue.Enqueue(delegate
                        {
                            writer.Dispose();
                            Interlocked.Decrement(ref counter);
                        });
                    }

                    _tasksQueue.Enqueue(delegate
                    {
                        while (Interlocked.Read(ref counter) != 0)
                            Thread.Sleep(1);
                        done.Set();
                    });
                });
            }

            private Item GetGroup(ushort groupId)
            {
                var group = _storage[groupId];
                if (Item.IsZero(group))
                    group = new Item(_buffersPool.GetBuffer());
                return group;
            }

            private IWriter GetWriter()
            {
                IWriter writer;
                if (!_writers.TryTake(out writer))
                    writer = _ioService.OpenWrite(_path);
                return writer;
            }

            private Item SaveData(Item group, byte[] buff, int offset, int length,
                Action bufferEnquedToWrite = null,
                Action bufferWritten = null)
            {
                var newOffset = group.BufferOffset + length;
                if (newOffset >= _bufferLength)
                {
                    var countToBuffEnd = _bufferLength - group.BufferOffset;
                    Array.Copy(buff, offset,
                               group.BufferHandle.Value, group.BufferOffset,
                               countToBuffEnd);

                    var oldBuffHandle = group.BufferHandle;
                    group.BufferHandle = _buffersPool.GetBuffer();

                    bufferEnquedToWrite?.Invoke();
                    _tasksQueue.Enqueue(delegate
                    {
                        var writer = GetWriter();
                        writer.Position = _fileOffset
                                        + Interlocked.Increment(ref _writedBuffersCount)
                                        * _bufferLength;

                        writer.Write(oldBuffHandle.Value, 0, _bufferLength);
                        _writers.Add(writer);

                        oldBuffHandle.Dispose();
                        bufferWritten?.Invoke();
                    });

                    newOffset = length - countToBuffEnd;
                    Array.Copy(buff, offset,
                               group.BufferHandle.Value, 0,
                               length - countToBuffEnd);
                }
                else
                    Array.Copy(buff, offset,
                               group.BufferHandle.Value, group.BufferOffset,
                               length);

                group.BufferOffset = newOffset;
                return group;
            }

            private struct Item
            {
                public Item(
                    IUsingHandle<byte[]> bufferHandle, 
                    int linesCount = 0)
                {
                    BufferHandle = bufferHandle;
                    LinesCount = linesCount;
                    BufferOffset = 0;
                }   
                                
                public IUsingHandle<byte[]> BufferHandle;
                public int BufferOffset, LinesCount;

                public static bool IsZero(Item item) =>
                    item.BufferHandle == null;
            }
        }
    }
}
