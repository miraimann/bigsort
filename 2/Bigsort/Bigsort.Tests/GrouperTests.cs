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
        [Timeout(20000)]
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
                {BufferSize.Min,   maxLineLength + 1},
                {BufferSize.Small, maxLineLength + 2},
                {BufferSize.Large, groupsFileLength + 1}
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

            IGroupInfoMonoid groupInfoMonoid = 
                new GroupInfoMonoid();

            IGroupsSummaryInfoMarger groupsSummaryInfoMarger = 
                new GroupsSummaryInfoMarger(groupInfoMonoid);

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
                .SplitToGroups(testCase.Lines)
                .ToArray();
            
            var summary = grouper
                .SplitToGroups(inputPath, groupsPath);
            
            Assert.AreEqual(
                expectedGroups.Max(group => group.LinesCount),
                summary.MaxGroupLinesCount);

            Assert.AreEqual(
                expectedGroups.Max(group => group.BytesCount),
                summary.MaxGroupSize);

            var resultGroups = ExtractGroups(summary, groupsFileContent);
            var expectedFroups = expectedGroups
                .OrderBy(group => group.Id)
                .ToArray();

            Assert.IsTrue(resultGroups.Select(Group.IsValid).All(o => o));
            CollectionAssert.AreEqual(expectedFroups, resultGroups);
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
                yield return BufferSize.Small;
                yield return BufferSize.Min;
            }
        }

        public enum BufferSize
        {
            Large = 0,
            Small = 1,
            Min = 2
        }
        
        private static Group[] ExtractGroups(
            IGroupsSummaryInfo groupsSummaryInfo,
            byte[] groupsContent)
        {
            var groups = groupsSummaryInfo.GroupsInfo;
            var result = new Group[groups.Count(o => o != null)];
            var resultPosition = 0;

            for (int i = 0; i < Consts.MaxGroupsCount; i++)
            {
                if (groups[i] != null)
                {
                    var lines = new List<Group.Line>();
                    result[resultPosition++] =
                        new Group(i)
                        {
                            BytesCount = groups[i].BytesCount,
                            LinesCount = groups[i].LinesCount,
                            Lines = lines
                        };

                    foreach (var blockRange in groups[i].Mapping)
                    {
                        var offset = (int) blockRange.Offset;
                        var overBlock = offset + (int) blockRange.Length;

                        while (offset != overBlock)
                        {
                            var lettersCount = groupsContent[offset];
                            var digitsCount = groupsContent[offset + 1];
                            var lineLength = digitsCount + lettersCount + 3;
                            var lineContent = new byte[lineLength];

                            Array.Copy(groupsContent, offset, lineContent, 0, lineLength);
                            lines.Add(new Group.Line(offset, lineContent));

                            offset += lineLength;
                        }
                    }
                }
            }

            return result;
        }


        public static IEnumerable<int> MaxThreadsCount =>
            Enumerable.Range(1, Environment.ProcessorCount - 1);
        
        public static IEnumerable<int> EnginesCount =>
            Enumerable.Range(1, Environment.ProcessorCount - 1);

        public static IEnumerable<TestCase> Cases =>
            new[]
                {
                    Cases_00_19
                }
                .Aggregate(Enumerable.Concat);
    }
}
