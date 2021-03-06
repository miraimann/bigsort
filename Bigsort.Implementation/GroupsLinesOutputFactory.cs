﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class GroupsLinesOutputFactory
        : IGroupsLinesOutputFactory
    {
        private readonly IIoService _ioService;
        private readonly ITasksQueue _tasksQueue;
        private readonly IBuffersPool _buffersPool;
        private readonly IConfig _config;

        public GroupsLinesOutputFactory(
            IIoService ioService,
            ITasksQueue tasksQueue,
            IBuffersPool buffersPool,
            IConfig config)
        {
            _ioService = ioService;
            _tasksQueue = tasksQueue;
            _buffersPool = buffersPool;
            _config = config;
        }

        public IGroupsLinesOutput Create(long fileOffset = 0) =>
            new LinesWriter(
                fileOffset,
                _buffersPool,
                _tasksQueue,
                _ioService,
                _config);

        private class LinesWriter
            : IGroupsLinesOutput
        {
            private readonly IIoService _ioService;
            private readonly IBuffersPool _buffersPool;
            private readonly ConcurrentBag<IFileWriter> _writers;
            private readonly ITasksQueue _tasksQueue;

            private readonly Group[] _groupsStorage;
            private readonly string _groupsFilePath;
            private readonly int _usingBufferLength;
            private long _writingPosition, _tasksCount;
            
            public LinesWriter(
                long fileOffset,
                IBuffersPool buffersPool,
                ITasksQueue tasksQueue, 
                IIoService ioService,
                IConfig config)
            {
                _buffersPool = buffersPool;
                _tasksQueue = tasksQueue;
                _ioService = ioService;
                _writingPosition = fileOffset;

                _usingBufferLength = config.UsingBufferLength;
                _groupsFilePath = config.GroupsFilePath;

                _groupsStorage = new Group[Consts.MaxGroupsCount];
                _writers = new ConcurrentBag<IFileWriter>();
            }
            
            public GroupInfo[] SelectSummaryGroupsInfo()
            {
                var result = new GroupInfo[Consts.MaxGroupsCount];
                for (int i = 0; i < Consts.MaxGroupsCount; i++)
                {
                    var group = _groupsStorage[i];
                    if (group != null)
                        result[i] = group.SelectGroupInfo();
                }

                return result;
            }

            public void ReleaseLine(ushort groupId,
                byte[] buff, int offset, int length)
            {
                var group = GetGroup(groupId);
                SaveData(group, buff, offset, length);
                ++group.LinesCount;
            }

            public void ReleaseBrokenLine(ushort groupId,
                byte[] leftBuff, int leftOffset, int leftLength,
                byte[] rightBuff, int rightLength)
            {
                var group = GetGroup(groupId);
                SaveData(group, leftBuff, leftOffset, leftLength);
                SaveData(group, rightBuff, 0, rightLength);
                ++group.LinesCount;
            }

            public void FlushAndDispose(CountdownEvent done)
            {
                var acc = new Group(_buffersPool.Get());
                for (int i = 0; i < Consts.MaxGroupsCount; i++)
                {
                    var group = _groupsStorage[i];
                    if (group == null)
                        continue;

                    FinalSaveData(acc, group);
                }

                if (acc.BufferOffset != 0)
                {
                    IncrementTasksCount();
                    _tasksQueue.Enqueue(delegate
                    {
                        var writer = GetWriter();
                        writer.Position = _writingPosition;
                        writer.Write(acc.BufferHandle.Value, 0, acc.BufferOffset);

                        _writers.Add(writer);
                        DecrementTasksCount();
                    });
                }

                Action disposeWriters = null;
                _tasksQueue.Enqueue(disposeWriters = delegate
                {
                    if (HasTasks())
                        _tasksQueue.Enqueue(disposeWriters);
                    else
                    {
                        foreach (var writer in _writers)
                            writer.Dispose();
                        done.Signal();
                    }
                });
            }

            private Group GetGroup(ushort groupId) =>
                _groupsStorage[groupId] ?? 
               (_groupsStorage[groupId] = new Group(_buffersPool.Get()));

            private void FinalSaveData(Group acc, Group group)
            {
                group.BytesCount += group.BufferOffset;
                group.Mapping.Add(
                    new LongRange(_writingPosition + acc.BufferOffset, 
                                  group.BufferOffset));

                var newOffset = acc.BufferOffset + group.BufferOffset;
                if (newOffset >= _usingBufferLength)
                {
                    var countToAccBuffEnd = _usingBufferLength - acc.BufferOffset;
                    Array.Copy(group.BufferHandle.Value, 0,
                               acc.BufferHandle.Value, acc.BufferOffset,
                               countToAccBuffEnd);
                    
                    var oldBuffHandle = acc.BufferHandle;
                    acc.BufferHandle = _buffersPool.Get();

                    newOffset = group.BufferOffset - countToAccBuffEnd;
                    Array.Copy(group.BufferHandle.Value, countToAccBuffEnd,
                               acc.BufferHandle.Value, 0,
                               newOffset);
                    
                    var positionMomento = _writingPosition;
                    _writingPosition += _usingBufferLength;

                    IncrementTasksCount();
                    _tasksQueue.Enqueue(delegate
                    {
                        var writer = GetWriter();
                        writer.Position = positionMomento;
                        writer.Write(oldBuffHandle.Value, 0, _usingBufferLength);
                            
                        oldBuffHandle.Dispose();
                        _writers.Add(writer);
                        DecrementTasksCount();
                    });
                }
                else
                    Array.Copy(group.BufferHandle.Value, 0,
                               acc.BufferHandle.Value, acc.BufferOffset,
                               group.BufferOffset);
                
                group.BufferHandle.Dispose();
                acc.BufferOffset = newOffset;
            }

            private void SaveData(Group group, 
                byte[] buff, int offset, int length)
            {
                var newOffset = group.BufferOffset + length;
                if (newOffset >= _usingBufferLength)
                {
                    var countToBuffEnd = _usingBufferLength - group.BufferOffset;
                    Array.Copy(buff, offset,
                               group.BufferHandle.Value, group.BufferOffset,
                               countToBuffEnd);

                    var oldBuffHandle = group.BufferHandle;
                    group.BufferHandle = _buffersPool.Get();
                    group.BytesCount += _usingBufferLength;

                    var positionMomento = _writingPosition;
                    _writingPosition += _usingBufferLength;
                    group.Mapping.Add(new LongRange(positionMomento, _usingBufferLength));

                    newOffset = length - countToBuffEnd;
                    Array.Copy(buff, offset + countToBuffEnd,
                               group.BufferHandle.Value, 0,
                               newOffset);

                    IncrementTasksCount();
                    _tasksQueue.Enqueue(delegate
                    {
                        var writer = GetWriter();
                        writer.Position = positionMomento;
                        writer.Write(oldBuffHandle.Value, 0, _usingBufferLength);

                        oldBuffHandle.Dispose();
                        _writers.Add(writer);
                        DecrementTasksCount();
                    });
                }
                else
                    Array.Copy(buff, offset,
                               group.BufferHandle.Value, group.BufferOffset,
                               length);

                group.BufferOffset = newOffset;
            }

            private IFileWriter GetWriter()
            {
                IFileWriter result;
                if (!_writers.TryTake(out result))
                    result = _ioService.OpenWrite(_groupsFilePath);

                return result;
            }

            private void IncrementTasksCount() =>
                Interlocked.Increment(ref _tasksCount);

            private void DecrementTasksCount() =>
                Interlocked.Decrement(ref _tasksCount);

            private bool HasTasks() =>
                Interlocked.Read(ref _tasksCount) != 0;

            private class Group
            {
                public Group(Handle<byte[]> bufferHandle)
                {
                    BufferHandle = bufferHandle;
                    Mapping = new List<LongRange>();
                }

                public int LinesCount, BytesCount, BufferOffset;
                
                public Handle<byte[]> BufferHandle;

                public List<LongRange> Mapping { get; }

                public GroupInfo SelectGroupInfo() =>
                    new GroupInfo
                    {
                        LinesCount = LinesCount,
                        BytesCount = BytesCount,
                        Mapping = Mapping
                    };
            }
        }
    }
}
