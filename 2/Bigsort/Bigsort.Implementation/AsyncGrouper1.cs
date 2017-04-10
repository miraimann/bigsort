using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class AsyncGrouper1
        : IGrouper
    { 
        private readonly IGrouperBuffersProviderMaker _buffersProviderMaker;
        private readonly ITasksQueueMaker _tasksQueueMaker;
        private readonly IIoService _ioService;
        private readonly IConfig _config;

        public AsyncGrouper1(
            IGrouperBuffersProviderMaker buffersProviderMaker,
            ITasksQueueMaker tasksQueueMaker,
            IIoService ioService,
            IConfig config)
        {
            _buffersProviderMaker = buffersProviderMaker;
            _tasksQueueMaker = tasksQueueMaker;
            _ioService = ioService;
            _config = config;
        }
        
        public IEnumerable<IGroupInfo> SplitToGroups(
            string filePath, 
            string outputDirectory = null)
        {
            string prevCurrentDirectory = null;
            if (outputDirectory != null)
            {
                prevCurrentDirectory = _ioService.CurrentDirectory;
                if (!Path.IsPathRooted(outputDirectory))
                    outputDirectory = Path.Combine(
                        prevCurrentDirectory,
                        outputDirectory);

                if (!_ioService.DirectoryExists(outputDirectory))
                    _ioService.CreateDirectory(outputDirectory);
                _ioService.CurrentDirectory = outputDirectory;
            }
            
            var tasksQueue = _tasksQueueMaker
                .MakePriorityQueue(Environment.ProcessorCount);

            var groupsStorage1 = new Dictionary<ushort, Group>(
                Consts.MaxGroupsCount);
            
            var groupsStorage2 = new Dictionary<ushort, Group>(
                Consts.MaxGroupsCount);
             
            long separationIndex = 0, fileLength = 0;
            using (var reader = _ioService.OpenPositionableRead(filePath))
            {
                fileLength = _ioService.SizeOfFile(filePath);
                separationIndex = fileLength / 2;
                var buff = new byte[600];
                reader.Possition = separationIndex;
                reader.Read(buff, 0, buff.Length);

                for (int i = 0;
                     buff[i] != Consts.EndLineByte1;
                     i++, separationIndex++)
                 ;
            }

            using (var buffersProvider1 = _buffersProviderMaker
                .Make(filePath, _config.BufferSize - 1, 0, separationIndex,
                    tasksQueue.AsHightQueue()))
            using (var buffersProvider2 = _buffersProviderMaker
                .Make(filePath, _config.BufferSize - 1, separationIndex, fileLength - separationIndex,
                    tasksQueue.AsHightQueue()))
            {
                var engine1 = new Engine(buffersProvider1, tasksQueue.AsLowQueue(),
                    groupsStorage1, _ioService, _config.BufferSize, "x");

                var engine2 = new Engine(buffersProvider2, tasksQueue.AsLowQueue(),
                    groupsStorage2, _ioService, _config.BufferSize, "o");

                ManualResetEvent
                    done1 = new ManualResetEvent(false),
                    done2 = new ManualResetEvent(false);

                engine1.Run(done1);
                engine2.Run(done2);

                WaitHandle.WaitAll(
                    new WaitHandle[] { done1, done2 });
            }

            if (prevCurrentDirectory != null)
                _ioService.CurrentDirectory = prevCurrentDirectory;

            return null;
            // return groupsStorage.Values.OrderBy(o => o.Name);
        }

        private class Engine
        {
            private const byte 
                EndLine = Consts.EndLineByte1,
                EndBuff = 1,
                EndStream = 0;

            private readonly string _groupFileNameMask;
            private readonly IGrouperBuffersProvider _buffersProvider;
            private readonly ITasksQueue _tasksQueue;
            private readonly IDictionary<ushort, Group> _groupsStorage;
            private readonly IIoService _ioService;
            private readonly int _buffLength;
            private readonly string _x;

            public Engine(
                IGrouperBuffersProvider buffersProvider, 
                ITasksQueue tasksQueue, 
                IDictionary<ushort, Group> groupsStorage, 
                IIoService ioService,
                int buffLength,
                string x)
            {
                _buffersProvider = buffersProvider;
                _tasksQueue = tasksQueue;
                _groupsStorage = groupsStorage;
                _ioService = ioService;
                _buffLength = buffLength;
                
                var ushortDigitsCount =
                    (int)Math.Ceiling(Math.Log10(ushort.MaxValue));
                _groupFileNameMask = new string('0', ushortDigitsCount);
                _x = x;
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
                                    id = (ushort) (c*byte.MaxValue);
                                    stage = Stage.ReadId;
                                }
                                else
                                {
                                    id += c;
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

                            IUsingHandle<byte[]> handle;
                            int count = _buffersProvider.TryGetNext(out handle);
                            if (count == Consts.TemporaryMissingResult)
                            {
                                _tasksQueue.Enqueue(() => Run(
                                    new State
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
                                    }));

                                return;
                            }

                            j += _buffLength;
                            i = 0;

                            disposePreviousBuff();
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
                                    disposeCurrentBuff();
                                    stage = Stage.Finish;
                                    break;
                                }

                                currentBuff[endStreamIndex] = EndStream;
                            }

                            stage = backStage;
                            break;

                        case Stage.ReleaseLine:

                            Group group;
                            if (!_groupsStorage.ContainsKey(id))
                            {
                                var name = id.ToString(_groupFileNameMask);
                                group = new Group(name,
                                    _ioService.OpenBufferingAsyncWrite(_x + name, _tasksQueue));

                                _groupsStorage.Add(id, group);
                            }
                            else group = _groupsStorage[id];

                            ++group.LinesCount;

                            var lineLength = digitsCount + lettersCount + 3;
                            var lineStart = i - lineLength;
                            var writer = group.Bytes;

                            if (lineStart < 0)
                            {
                                lineLength = Math.Abs(lineStart);
                                lineStart += previousBuff.Length - 1; // _buffLength - 1;

                                previousBuff[lineStart] = (byte) lettersCount;
                                if (lineLength > 1)
                                    previousBuff[lineStart + 1] = (byte) digitsCount;
                                else currentBuff[0] = (byte) digitsCount;

                                writer.Write(previousBuff, lineStart, lineLength);
                                writer.Write(currentBuff, 0, i);
                            }
                            else
                            {
                                currentBuff[lineStart] = (byte) lettersCount;
                                currentBuff[lineStart + 1] = (byte) digitsCount;
                                writer.Write(currentBuff, lineStart, lineLength);
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

                            foreach (Group g in _groupsStorage.Values)
                                g.Bytes.Flush();

                            var allWritingsDone = new ManualResetEvent(false);
                            _tasksQueue.Enqueue(() => allWritingsDone.Set());

                            allWritingsDone.WaitOne();
                            
                            foreach (Group g in _groupsStorage.Values)
                                _tasksQueue.Enqueue(g.Bytes.Dispose);

                            _tasksQueue.Enqueue(() => state.Done.Set());

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

            public enum Stage
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

        private class Group
            : IGroupInfo
        {
            public Group(string name, IWriter writer)
            {
                Name = name;
                Bytes = writer;
            }

            public string Name { get; }
            public IWriter Bytes { get; set; }
            public int LinesCount { get; set; }
            public int BytesCount { get; set; }
        }
    }
}
