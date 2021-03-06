﻿using System;
using System.Linq;
using System.Threading;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class Grouper
        : IGrouper
    {
        private readonly IGroupsInfoMarger _groupsInfoMarger;
        private readonly IGrouperIOs _ios;
        private readonly ITasksQueue _tasksQueue;
        private readonly int _usingBufferLength;

        public Grouper(
            IGroupsInfoMarger groupsInfoMarger,
            IGrouperIOs grouperIOs,
            ITasksQueue tasksQueue,
            IConfig config)
        {
            _groupsInfoMarger = groupsInfoMarger;
            _ios = grouperIOs;
            _tasksQueue = tasksQueue;
            _usingBufferLength = config.UsingBufferLength;
        }

        public GroupInfo[] SeparateInputToGroups()
        {
            var enginesCount = _ios.Count;
            var done = new CountdownEvent(enginesCount);

            for (int i = 0; i < enginesCount; i++)
            {
                var engine = new Engine(_tasksQueue, _ios[i], _usingBufferLength);
                _tasksQueue.Enqueue(() => engine.Run(done));
            }

            done.Wait();
            
            return _groupsInfoMarger.Marge(
                _ios.Select(io => io.Output.SelectSummaryGroupsInfo())
                    .ToArray());
        }

        private class Engine
        {
            private const byte
                FirstIdByteOffset = Consts.AsciiPrintableCharsOffset,
                SecondIdByteOffset = Consts.AsciiPrintableCharsOffset - 1,
                FirstIdByteMultiper = Consts.AsciiPrintableCharsCount + 1,
                EndLine = Consts.EndLineByte1,
                EndBuff = 1,
                EndStream = 0;

            private readonly IGrouperIO _io;
            private readonly ITasksQueue _tasksQueue;
            private readonly int _usingBufferLength;

            public Engine(
                ITasksQueue tasksQueue,
                IGrouperIO io,
                int usingBufferLength)
            {
                _io = io;
                _tasksQueue = tasksQueue;
                _usingBufferLength = usingBufferLength;
            }

            public void Run(CountdownEvent done)
            {
                Handle<byte[]> firstBufferHandle;
                var firstBufferLength = _io.Input.GetFirstBuffer(out firstBufferHandle);
                firstBufferHandle.Value[firstBufferLength] =
                    firstBufferLength == _usingBufferLength - 1
                        ? EndBuff
                        : EndStream;

                Run(new State
                {
                    LettersCount = 0,
                    DigitsCount = 0,
                    CurrentGroupId = 0,
                    Iterator = Consts.EndLineBytesCount,
                    Anchor = Consts.EndLineBytesCount,

                    DisposeCurrentBuff = firstBufferHandle.Dispose,
                    DisposePreviousBuff = Consts.ZeroAction,

                    CurrentBuff = firstBufferHandle.Value,
                    PreviousBuff = null,

                    CurrentStage = Stage.ReadNumber,
                    BackStage = Stage.None,
                    Done = done
                });
            }

            private void Run(State state)
            {
                int usingBufferLength = _usingBufferLength,
                    lettersCount = state.LettersCount,
                    digitsCount = state.DigitsCount,
                    i = state.Iterator,
                    j = state.Anchor;

                ushort id = state.CurrentGroupId;

                byte[] currentBuff = state.CurrentBuff,
                       previousBuff = state.PreviousBuff;

                Action disposeCurrentBuff = state.DisposeCurrentBuff,
                       disposePreviousBuff = state.DisposePreviousBuff;

                Stage backStage = state.BackStage,
                      stage = state.CurrentStage;

                byte c;

                while (true)
                {
                    switch (stage)
                    {
                        case Stage.ReadNumber:

                            while (currentBuff[i] > Consts.Dot)
                                i++;

                            if (j < usingBufferLength)
                                digitsCount += i - j;

                            if (currentBuff[i] == Consts.Dot)
                            {
                                if (j > usingBufferLength)
                                    digitsCount += i;

                                j = ++i;
                                stage = Stage.ReadId;
                                break;
                            }

                            // endBuff
                            backStage = Stage.ReadNumber;
                            stage = Stage.LoadNextBuff;
                            break;

                        case Stage.ReadId:

                            var readFirstLetter = id == 0;
                            c = currentBuff[i];

                            if (c > EndLine)
                            {
                                if (readFirstLetter)
                                {
                                    stage = Stage.ReadId;
                                    id = (ushort) ((c - FirstIdByteOffset)
                                        * FirstIdByteMultiper
                                        + 1);
                                }
                                else
                                {
                                    id += (ushort) (c - SecondIdByteOffset);
                                    stage = Stage.ReadString;
                                }

                                ++i;
                                break;
                            }

                            lettersCount = readFirstLetter ? 0 : 1;
                            if (c == EndLine)
                            {
                                stage = Stage.ReleaseLine;
                                break;
                            }

                            // endBuff
                            backStage = Stage.ReadId;
                            stage = Stage.LoadNextBuff;
                            break;

                        case Stage.ReadString:

                            while (currentBuff[i] > EndLine)
                                i++;

                            if (j < usingBufferLength)
                                lettersCount += i - j;

                            if (currentBuff[i] == EndLine)
                            {
                                if (j > usingBufferLength)
                                    lettersCount += i;

                                stage = Stage.ReleaseLine;
                                break;
                            }

                            // endBuff
                            backStage = Stage.ReadString;
                            stage = Stage.LoadNextBuff;
                            break;

                        case Stage.LoadNextBuff:

                            disposePreviousBuff?.Invoke();
                            disposePreviousBuff = null;

                            Handle<byte[]> handle;
                            int count = _io.Input.TryGetNextBuffer(out handle);
                            if (count == Consts.TemporaryMissingResult)
                            {
                                var momento = new State
                                {
                                    LettersCount = lettersCount,
                                    DigitsCount = digitsCount,
                                    CurrentGroupId = id,
                                    Iterator = i,
                                    Anchor = j,

                                    DisposeCurrentBuff = disposeCurrentBuff,
                                    DisposePreviousBuff = disposePreviousBuff,

                                    CurrentBuff = currentBuff,
                                    PreviousBuff = previousBuff,

                                    CurrentStage = stage,
                                    BackStage = backStage,
                                    Done = state.Done
                                };

                                _tasksQueue.Enqueue(() =>
                                        Run(momento));
                                return;
                            }

                            j += usingBufferLength;
                            i = 0;

                            disposePreviousBuff = disposeCurrentBuff;
                            disposeCurrentBuff = handle.Dispose;

                            previousBuff = currentBuff;
                            currentBuff = handle.Value;

                            if (count == usingBufferLength - 1)
                                currentBuff[usingBufferLength - 1] = EndBuff;
                            else
                            {
                                var endStreamIndex = Math.Max(0, count - 1);
                                if (endStreamIndex == 0)
                                {
                                    stage = Stage.Finish;
                                    break;
                                }

                                currentBuff[endStreamIndex] = EndStream;
                            }

                            stage = backStage;
                            break;

                        case Stage.ReleaseLine:

                            var lineLength = digitsCount + lettersCount + 3;
                            var lineStart = i - lineLength;

                            if (lineStart < 0)
                            {
                                lineLength = Math.Abs(lineStart);
                                lineStart += _usingBufferLength - 1;

                                previousBuff[lineStart] = (byte) lettersCount;
                                if (lineLength > 1)
                                    previousBuff[lineStart + 1] = (byte) digitsCount;
                                else currentBuff[0] = (byte) digitsCount;

                                _io.Output.ReleaseBrokenLine(id,
                                    previousBuff, lineStart, lineLength,
                                    currentBuff, i);
                            }
                            else
                            {
                                currentBuff[lineStart] = (byte) lettersCount;
                                currentBuff[lineStart + 1] = (byte) digitsCount;
                                _io.Output.ReleaseLine(id, currentBuff, lineStart, lineLength);
                            }

                            lettersCount = 0;
                            digitsCount = 0;
                            id = 0;

                            if (currentBuff[++i] == EndBuff)
                            {
                                backStage = Stage.CheckFinish;
                                stage = Stage.LoadNextBuff;
                                break;
                            }

                            stage = Stage.CheckFinish;
                            break;

                        case Stage.CheckFinish:
                            stage = currentBuff[i++] == EndStream
                                ? Stage.Finish
                                : Stage.ReadNumber;
                            j = i;
                            break;

                        case Stage.Finish:

                            disposeCurrentBuff();
                            _io.Output.FlushAndDispose(state.Done);
                            _io.Input.Dispose();
                            return;
                    }
                }
            }

            private struct State
            {
                public Stage CurrentStage, BackStage;

                public byte[] CurrentBuff, PreviousBuff;

                public Action
                    DisposeCurrentBuff,
                    DisposePreviousBuff;

                public ushort CurrentGroupId;

                public int
                    LettersCount,
                    DigitsCount,
                    Iterator,
                    Anchor;

                public CountdownEvent Done;
            }

            private enum Stage
            {
                ReadNumber,
                ReadId,
                ReadString,
                ReleaseLine,
                LoadNextBuff,
                CheckFinish,
                Finish,
                None
            }
        }
    }
}
