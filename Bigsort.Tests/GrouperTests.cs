using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bigsort.Contracts;
using Bigsort.Implementation;
using Moq;
using NUnit.Framework;

namespace Bigsort.Tests
{
    [TestFixture]
    public partial class GrouperTests
    {
        [Test]
        [Parallelizable]
        [Timeout(10000)]
        public void Test(
            [ValueSource(nameof(Cases))] TestCase testCase,
            [ValueSource(nameof(BufferSizes))] BufferSize bufferSize,
            [ValueSource(nameof(EnginesCount))] int enginesCount,
            [ValueSource(nameof(MaxThreadsCount))] int maxThreadsCount)
        {
            const string inputPath = "ZZZZZzzzzZzZZzzzZZZzzz",
                        groupsPath = "WWwwwWWwwwWWWwwwwwwwww";
            
            var groupsFileLength = testCase.Lines
                .Sum(o => o.Length + Consts.EndLineBytesCount);

            byte[] groupsFileContent = new byte[groupsFileLength];
            var inputSource = testCase.Lines
                .SelectMany(line => new[] {line.Select(c => (byte) c), Consts.EndLineBytes})
                .Aggregate(Enumerable.Concat)
                .ToArray();

            var ioServiceMock = new Mock<IIoService>();
            var configMock = new Mock<IConfig>();

            ioServiceMock
                .Setup(o => o.SizeOfFile(inputPath))
                .Returns(groupsFileLength);

            ioServiceMock
                .Setup(o => o.OpenRead(inputPath, It.IsAny<long>()))
                .Returns((string _, long position) =>
                {
                    var inputStream = new MemoryStream(inputSource);
                    var inputReaderMock = new Mock<IFileReader>();
                    inputReaderMock
                        .SetupGet(o => o.Length)
                        .Returns(() => inputStream.Length);

                    inputReaderMock
                        .SetupGet(o => o.Position)
                        .Returns(() => inputStream.Position);

                    inputReaderMock
                        .SetupSet(o => o.Position = It.IsAny<long>())
                        .Callback((long value) => inputStream.Position = value);

                    inputReaderMock
                        .Setup(o => o.ReadByte())
                        .Returns(() => inputStream.ReadByte());

                    inputReaderMock
                        .Setup(o => o.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                        .Returns((byte[] buff, int offset, int count) =>
                                inputStream.Read(buff, offset, count));

                    inputReaderMock
                        .Setup(o => o.Dispose())
                        .Callback(() => inputStream.Dispose());

                    inputReaderMock.Object.Position = position;
                    return inputReaderMock.Object;
                });

            ioServiceMock
                .Setup(o => o.OpenWrite(groupsPath, It.IsAny<long>(), false))
                .Returns((string _, long position, bool __) =>
                {
                    var groupsStream = new MemoryStream(groupsFileContent);
                    var writerMock = new Mock<IFileWriter>();

                    writerMock
                        .SetupGet(o => o.Position)
                        .Returns(() => groupsStream.Position);

                    writerMock
                        .SetupSet(o => o.Position = It.IsAny<long>())
                        .Callback((long value) => groupsStream.Position = value);

                    writerMock
                        .SetupGet(o => o.Length)
                        .Returns(() => groupsStream.Length);   

                    writerMock
                        .Setup(o => o.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                        .Callback((byte[] buff, int offset, int count) =>
                                groupsStream.Write(buff, offset, count));

                    writerMock
                        .Setup(o => o.Dispose())
                        .Callback(() => groupsStream.Close());

                    return writerMock.Object;
                });

            var maxLineLength = testCase.Lines
                .Max(line => line.Length + Consts.EndLineBytesCount);

            int buffSize = new Dictionary<BufferSize, int>
            {
                {BufferSize.Min,    maxLineLength + 1},
                {BufferSize.Small,  maxLineLength + 2},
                {BufferSize.Medium, groupsFileLength + 1},
                {BufferSize.Large,  groupsFileLength * 2}
            }[bufferSize];

            configMock
                .SetupGet(o => o.BufferSize)
                .Returns(buffSize);

            configMock
                .SetupGet(o => o.MaxRunningTasksCount)
                .Returns(maxThreadsCount);

            configMock
                .SetupGet(o => o.GrouperEnginesCount)
                .Returns(enginesCount);
            
            IGroupsSummaryInfoMarger groupsSummaryInfoMarger = 
                new GroupsSummaryInfoMarger();

            IUsingHandleMaker usingHandleMaker =
                new UsingHandleMaker();

            IPoolMaker poolMaker = 
                new PoolMaker(usingHandleMaker);

            ITasksQueue tasksQueue =
                new TasksQueue(configMock.Object);

            IBuffersPool buffersPool = 
                new BuffersPool(poolMaker, configMock.Object);
            
            IGrouperBuffersProviderMaker grouperBuffersProviderMaker =
                new GrouperBuffersProviderMaker(
                    buffersPool,
                    ioServiceMock.Object,
                    usingHandleMaker,
                    tasksQueue,
                    configMock.Object);

            IGroupsLinesWriterMaker linesWriterMaker =
                new GroupsLinesWriterMaker(
                    ioServiceMock.Object,
                    buffersPool,
                    tasksQueue,
                    configMock.Object);

            IGrouperIOMaker grouperIoMaker = 
                new GrouperIOMaker(
                    grouperBuffersProviderMaker,
                    linesWriterMaker,
                    ioServiceMock.Object,
                    configMock.Object); 

            var grouper = new Grouper(
                groupsSummaryInfoMarger,
                grouperIoMaker,
                tasksQueue,
                configMock.Object);

            var trivialGrouper = new TrivialGrouper();
            var expectedGroups = trivialGrouper
                .SplitToGroups(testCase.Lines);
            
            var summary = grouper
                .SplitToGroups(inputPath, groupsPath);
            
            Assert.AreEqual(
                expectedGroups.Max(group => group.LinesCount),
                summary.MaxGroupLinesCount);

            Assert.AreEqual(
                expectedGroups.Max(group => group.BytesCount),
                summary.MaxGroupSize);

            var resultGroups = ExtractGroups(summary, groupsFileContent);

            Assert.IsTrue(resultGroups.Select(Group.IsValid).All(o => o));
            CollectionAssert.AreEqual(
                expectedGroups, 
                resultGroups);
        }

        public class TestCase
        {
            private readonly string _name;
            public TestCase(string name, string[] lines)
            {
                _name = name;
                Lines = lines;
            }

            public string[] Lines { get; }
            
            public override string ToString() =>
                _name;
        }
        
        public static IEnumerable<BufferSize> BufferSizes
        {
            get
            {
                yield return BufferSize.Large;
                yield return BufferSize.Medium;
                yield return BufferSize.Small;
                yield return BufferSize.Min;
            }
        }

        public enum BufferSize
        {
            Large,
            Medium,
            Small,
            Min
        }
        
        private static Group[] ExtractGroups(
            IGroupsSummaryInfo groupsSummaryInfo,
            byte[] groupsContent)
        {
            const int unknown = -1;

            var groups = groupsSummaryInfo.GroupsInfo;
            var result = new Group[groups.Count(o => !GroupInfo.IsZero(o))];
            var resultPosition = 0;

            for (int i = 0; i < Consts.MaxGroupsCount; i++)
            {
                if (!GroupInfo.IsZero(groups[i]))
                {
                    var lines = new List<Group.Line>();
                    result[resultPosition++] =
                        new Group(i)
                        {
                            BytesCount = groups[i].BytesCount,
                            LinesCount = groups[i].LinesCount,
                            Lines = lines
                        };

                    byte[] lineContent = null;
                    int lineRemainder = 0;
                    byte lettersCount = 0;

                    foreach (var blockRange in groups[i].Mapping)
                    {
                        var offset = (int) blockRange.Offset;
                        var overBlock = offset + (int) blockRange.Length;
                        
                        while (offset != overBlock)
                        {
                            if (lineRemainder == 0)
                            {
                                lettersCount = groupsContent[offset];

                                if (offset + 1 != overBlock)
                                {
                                    var digitsCount = groupsContent[offset + 1];
                                    var lineLength = digitsCount + lettersCount + 3;
                                    lineContent = new byte[lineLength];

                                    var lengthToBlockEnd = overBlock - offset;
                                    var lengthToCopy = Math.Min(lengthToBlockEnd, lineLength);

                                    Array.Copy(groupsContent, offset, lineContent, 0, lengthToCopy);

                                    if (lengthToCopy == lineLength)
                                        lines.Add(new Group.Line(offset, lineContent));
                                    else lineRemainder = lineLength - lengthToCopy;

                                    offset += lengthToCopy;
                                }
                                else
                                {
                                    lineRemainder = unknown;
                                    ++offset;
                                }
                            }
                            else
                            {
                                if (lineRemainder == unknown)
                                {
                                    var digitsCount = groupsContent[offset];
                                    var linelength = digitsCount + lettersCount + 3;
                                    lineContent = new byte[linelength];
                                    lineContent[0] = lettersCount;
                                    lineRemainder = linelength - 1;
                                }

                                var offsetInLine = lineContent.Length - lineRemainder;
                                Array.Copy(groupsContent, offset, 
                                           lineContent, offsetInLine, 
                                           lineRemainder);

                                lines.Add(new Group.Line(offset - offsetInLine, lineContent));

                                offset += lineRemainder;
                                lineRemainder = 0;
                            }
                        }
                    }
                }
            }

            return result;
        }


        public static IEnumerable<int> MaxThreadsCount =>
            Enumerable.Range(1, Environment.ProcessorCount);
        
        public static IEnumerable<int> EnginesCount =>
            Enumerable.Range(1, Environment.ProcessorCount);

        public static IEnumerable<TestCase> Cases =>
            new[]
                {
                    Cases_00_19
                }
                .Aggregate(Enumerable.Concat);

        private static int ToId(string prefix)
        {
            var id = 0;
            if (prefix.Length == 0)
                return id;

            id = (prefix[0] - Consts.AsciiPrintableCharsOffset)
               * (Consts.AsciiPrintableCharsCount + 1)
               + 1;

            if (prefix.Length == 1)
                return id;
            
            id ++;
            id += prefix[1];
            id -= Consts.AsciiPrintableCharsOffset;

            return id;
        }

        private static string ToPrefix(int id)
        {
            if (id == 0)
                return string.Empty;

            int c1 = (id - 1) / (Consts.AsciiPrintableCharsCount + 1),
                c2 = (id - 1) % (Consts.AsciiPrintableCharsCount + 1);

            if (c2 == 0)
                return ((char) (c1 + Consts.AsciiPrintableCharsOffset))
                    .ToString();

            return new string(new []{c1, c2 - 1}
                .Select(c => (char) (c + Consts.AsciiPrintableCharsOffset))
                .ToArray());
        }
    }
}
