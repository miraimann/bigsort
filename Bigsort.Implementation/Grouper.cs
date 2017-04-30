using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation
{
    public class GrouperMaker
        : IGrouperMaker
    {
        private readonly ITimeTracker _timeTracker;
        private readonly IGroupsSummaryInfoMarger _summaryInfoMarger;
        private readonly IGrouperIOMaker _grouperIoMaker;
        private readonly ITasksQueue _tasksQueue;
        private readonly IConfig _config;

        public GrouperMaker(
            IGroupsSummaryInfoMarger summaryInfoMarger,
            IGrouperIOMaker grouperIoMaker,
            ITasksQueue tasksQueue,
            IConfig config,
            IDiagnosticTools diagnosticTool = null)
        {
            _summaryInfoMarger = summaryInfoMarger;
            _grouperIoMaker = grouperIoMaker;
            _tasksQueue = tasksQueue;
            _config = config;
            _timeTracker = diagnosticTool?.TimeTracker;
        }

        public IGrouper Make(IPool<byte[]> buffersPool) =>
            new Grouper(
                buffersPool,
                _summaryInfoMarger,
                _grouperIoMaker,
                _tasksQueue,
                _config,
                _timeTracker);

        private class Grouper
            : IGrouper
        {
            public const string
                LogName = nameof(Grouper),
                GroupingLogName = LogName + "." + nameof(SplitToGroups);

            private readonly ITimeTracker _timeTracker;
            private readonly IPool<byte[]> _buffersPool;
            private readonly IGroupsSummaryInfoMarger _summaryInfoMarger;
            private readonly IGrouperIOMaker _grouperIoMaker;
            private readonly ITasksQueue _tasksQueue;
            private readonly IConfig _config;

            public Grouper(
                IPool<byte[]> buffersPool,
                IGroupsSummaryInfoMarger summaryInfoMarger,
                IGrouperIOMaker grouperIoMaker,
                ITasksQueue tasksQueue,
                IConfig config,
                ITimeTracker timeTracker)
            {
                _buffersPool = buffersPool;
                _summaryInfoMarger = summaryInfoMarger;
                _grouperIoMaker = grouperIoMaker;
                _tasksQueue = tasksQueue;
                _config = config;
                _timeTracker = timeTracker;
            }

            public IGroupsSummaryInfo SplitToGroups(
                string inputPath, string groupsFile)
            {
                var watch = Stopwatch.StartNew();

                var enginesCount = _config.GrouperEnginesCount;
                var ios = enginesCount == 1
                    ? new[] {_grouperIoMaker.Make(inputPath, groupsFile, _buffersPool)}
                    : _grouperIoMaker.MakeMany(inputPath, groupsFile, enginesCount, _buffersPool);

                var engines = ios
                    .Select(io => new Engine(_tasksQueue, io, _config.PhysicalBufferLength))
                    .ToArray();

                enginesCount = engines.Length;

                var doneEvents = Enumerable
                    .Range(0, enginesCount)
                    .Select(_ => new ManualResetEvent(false))
                    .ToArray();

                for (int i = 0; i < enginesCount; i++)
                {
                    var j = i;
                    _tasksQueue.Enqueue(() =>
                            engines[j].Run(doneEvents[j]));
                }

                WaitHandle.WaitAll(doneEvents);

                var summary = new GroupInfo[enginesCount][];
                for (int i = 0; i < enginesCount; i++)
                    summary[i] = ios[i].Output.SelectSummaryGroupsInfo();

                _timeTracker?.Add(GroupingLogName, watch.Elapsed);
                return _summaryInfoMarger.Marge(summary);
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
                private readonly int _buffLength;

                public Engine(
                    ITasksQueue tasksQueue,
                    IGrouperIO io,
                    int buffLength)
                {
                    _io = io;
                    _tasksQueue = tasksQueue;
                    _buffLength = buffLength;
                }

                public void Run(ManualResetEvent done)
                {
                    var newLineLength = Environment.NewLine.Length;
                    var startingBuff = new byte[newLineLength + 1];
                    startingBuff[newLineLength] = EndBuff;

                    Run(new State
                    {
                        LettersCount = 0,
                        DigitsCount = 0,
                        CurrentGroupId = 0,
                        Iterator = newLineLength,
                        Anchor = newLineLength,

                        DisposeCurrentBuff = Consts.ZeroAction,
                        DisposePreviousBuff = Consts.ZeroAction,

                        CurrentBuff = startingBuff,
                        PreviousBuff = null,

                        CurrentStage = Stage.ReadNumber,
                        BackStage = Stage.None,
                        Done = done
                    });
                }

                private void Run(State state)
                {
                    int lettersCount = state.LettersCount,
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

                                if (j < _buffLength)
                                    digitsCount += i - j;

                                if (currentBuff[i] == Consts.Dot)
                                {
                                    if (j > _buffLength)
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

                                if (j < _buffLength)
                                    lettersCount += i - j;

                                if (currentBuff[i] == EndLine)
                                {
                                    if (j > _buffLength)
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

                                IUsingHandle<byte[]> handle;
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

                                j += _buffLength;
                                i = 0;

                                disposePreviousBuff = disposeCurrentBuff;
                                disposeCurrentBuff = handle.Dispose;

                                previousBuff = currentBuff;
                                currentBuff = handle.Value;

                                if (count == _buffLength - 1)
                                    currentBuff[_buffLength - 1] = EndBuff;
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
                                    lineStart += previousBuff.Length - 1; // _buffLength - 1;

                                    previousBuff[lineStart] = (byte) lettersCount;
                                    if (lineLength > 1)
                                        previousBuff[lineStart + 1] = (byte) digitsCount;
                                    else currentBuff[0] = (byte) digitsCount;

                                    _io.Output.AddBrokenLine(id,
                                        previousBuff, lineStart, lineLength,
                                        currentBuff, 0, i);
                                }
                                else
                                {
                                    currentBuff[lineStart] = (byte) lettersCount;
                                    currentBuff[lineStart + 1] = (byte) digitsCount;
                                    _io.Output.AddLine(id, currentBuff, lineStart, lineLength);
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

                    public ManualResetEvent Done;
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
}
