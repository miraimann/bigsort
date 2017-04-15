using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            ITasksQueue tasksQueue, 
            IConfig config)
        {
            _ioService = ioService;
            _buffersPool = buffersPool;
            _config = config;
            _tasksQueue = tasksQueue;
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

            private readonly ConcurrentBag<IFileWriter> _writers;
            private readonly Group[] _groupsStorage;
            private readonly int _bufferLength;
            private readonly string _path;
            private long _writingPosition;

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
                _writingPosition = fileOffset;
                _bufferLength = config.BufferSize;

                _groupsStorage = new Group[Consts.MaxGroupsCount];
                _writers = new ConcurrentBag<IFileWriter>(
                    Enumerable.Range(0, InitWritersCount)
                              .Select(_ => _ioService.OpenWrite(path)));
            }

            public IReadOnlyList<IGroupInfo> SummaryGroupsInfo =>
                _groupsStorage;

            public void AddLine(ushort groupId,
                byte[] buff, int offset, int length)
            {
                var group = GetGroup(groupId);
                SaveData(group, buff, offset, length);
                ++group.LinesCount;
            }

            public void AddBrokenLine(ushort groupId,
                byte[] leftBuff, int leftOffset, int leftLength,
                byte[] rightBuff, int rightOffset, int rightLength)
            {
                var group = GetGroup(groupId);
                SaveData(group, leftBuff, leftOffset, leftLength);
                SaveData(group, rightBuff, rightOffset, rightLength);
                ++group.LinesCount;
            }

            public void FlushAndDispose(ManualResetEvent done)
            {
                long counter = 0;
                Action decrementCounter = () => Interlocked.Decrement(ref counter),
                       incrementCounter = () => Interlocked.Increment(ref counter);

                var acc = new Group(_buffersPool.GetBuffer());

                for (int i = 0; i < Consts.MaxGroupsCount; i++)
                {
                    var group = _groupsStorage[i];
                    if (group == null)
                        continue;

                 // if buffer was send to write
                    if (FinalSaveData(acc, group, decrementCounter))
                    {
                        incrementCounter();
                        _writingPosition += _bufferLength;
                    }
                }

                if (acc.BufferOffset != 0)
                {
                    incrementCounter();
                    _tasksQueue.Enqueue(delegate
                    {
                        var writer = GetWriter();
                        writer.Position = _writingPosition;
                        writer.Write(acc.BufferHandle.Value,
                                     0, acc.BufferOffset);

                        decrementCounter();
                        writer.Dispose();
                    });
                }

                Action disposeWriters = null;
                _tasksQueue.Enqueue(disposeWriters = delegate
                {
                    if (Interlocked.Read(ref counter) != 0)
                    {
                        _tasksQueue.Enqueue(disposeWriters);
                        return;
                    }

                    foreach (var writer in _writers)
                    {
                        incrementCounter();
                        _tasksQueue.Enqueue(delegate
                        {
                            writer.Dispose();
                            decrementCounter();
                        });
                    }

                    Action checkDone = null;
                    _tasksQueue.Enqueue(checkDone = delegate
                    {
                        if (Interlocked.Read(ref counter) != 0)
                            _tasksQueue.Enqueue(checkDone);
                        else done.Set();
                    });
                });
            }

            private Group GetGroup(ushort groupId) =>
                _groupsStorage[groupId] ?? 
               (_groupsStorage[groupId] = new Group(_buffersPool.GetBuffer()));

            private IFileWriter GetWriter()
            {
                IFileWriter writer;
                if (!_writers.TryTake(out writer))
                    writer = _ioService.OpenWrite(_path);
                return writer;
            }

            private bool FinalSaveData(Group acc, Group group, Action bufferWritten)
            {
                bool bufferWasEnqueuedToWrite = false;
                group.BytesCount += group.BufferOffset;
                group.MappingAccumulator.Add(
                    new LongRange(_writingPosition + acc.BufferOffset, 
                                  group.BufferOffset));

                var newOffset = acc.BufferOffset + group.BufferOffset;
                if (newOffset >= _bufferLength)
                {
                    var countToAccBuffEnd = _bufferLength - acc.BufferOffset;
                    Array.Copy(group.BufferHandle.Value, 0,
                               acc.BufferHandle.Value, acc.BufferOffset,
                               countToAccBuffEnd);
                    
                    var oldBuffHandle = acc.BufferHandle;
                    acc.BufferHandle = _buffersPool.GetBuffer();

                    newOffset = group.BufferOffset - countToAccBuffEnd;
                    Array.Copy(group.BufferHandle.Value, countToAccBuffEnd,
                               acc.BufferHandle.Value, 0,
                               newOffset);

                    bufferWasEnqueuedToWrite = true;
                    var positionMomento = _writingPosition;
                    _tasksQueue.Enqueue(delegate
                    {
                        var writer = GetWriter();
                        writer.Position = positionMomento;
                        writer.Write(oldBuffHandle.Value, 0, _bufferLength);

                        _writers.Add(writer);
                        oldBuffHandle.Dispose();
                        bufferWritten();
                    });
                }
                else
                    Array.Copy(group.BufferHandle.Value, 0,
                               acc.BufferHandle.Value, acc.BufferOffset,
                               group.BufferOffset);
                
                group.BufferHandle.Dispose();
                acc.BufferOffset = newOffset;
                return bufferWasEnqueuedToWrite;
            }

            private void SaveData(Group group, 
                byte[] buff, int offset, int length)
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
                    group.BytesCount += _bufferLength;

                    var positionMomento = _writingPosition += _bufferLength;
                    group.MappingAccumulator
                         .Add(new LongRange(positionMomento, _bufferLength));

                    newOffset = length - countToBuffEnd;
                    Array.Copy(buff, offset + countToBuffEnd,
                               group.BufferHandle.Value, 0,
                               newOffset);

                    _tasksQueue.Enqueue(delegate
                    {
                        var writer = GetWriter();
                        writer.Position = positionMomento;
                        writer.Write(oldBuffHandle.Value, 0, _bufferLength);

                        _writers.Add(writer);
                        oldBuffHandle.Dispose();
                    });
                }
                else
                    Array.Copy(buff, offset,
                               group.BufferHandle.Value, group.BufferOffset,
                               length);

                group.BufferOffset = newOffset;
            }

            private class Group
                : IGroupInfo
            {
                public Group(IUsingHandle<byte[]> bufferHandle)
                {
                    BufferHandle = bufferHandle;
                    MappingAccumulator = new List<LongRange>();
                }
                
                public int BufferOffset { get; set; }

                public IUsingHandle<byte[]> BufferHandle { get; set; }

                public List<LongRange> MappingAccumulator { get; }

                public IEnumerable<LongRange> Mapping =>
                    MappingAccumulator;

                public int LinesCount { get; set; }
                public int BytesCount { get; set; }
            }
        }
    }
}
