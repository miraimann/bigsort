using System.IO;
using System.Linq;
using Bigsort.Contracts;
using Bigsort.Implementation;
using Moq;
using static Bigsort.Tests.Tools;
using NUnit.Framework;
using Range = Bigsort.Contracts.Range;

namespace Bigsort.Tests
{
    [TestFixture]
    public class SortedGroupWriterTests
    {
        [TestCase(11, 1, 22, 1024, 0)]
        [TestCase(11, 1 ,2, 11, 0)]
        [TestCase(2222, 1, 2, 11, 0)]
        public void DebugTest(
                int linesCount,
                int maxNumberLength,
                int maxStingLength,
                int bufferSize,
                int readingEnsurance) =>
            Test(linesCount,
                maxNumberLength,
                maxStingLength,
                bufferSize,
                readingEnsurance);

        [Test]
        [Parallelizable]
        [Timeout(10000)]
        public void Test(
            [Values(1, 11, 111, 222, 2222)] int linesCount,
            [Values(1, 2, 3, 22, 32, 255)] int maxNumberLength,
            [Values(2, 3, 4, 22, 64, 255)] int maxStingLength,
            [Values(8, 9, 10, 11, 12, 13, 14, 15, 16, 1024, 32*1024)] int bufferSize,
            [Values(0, 1, 2, 3, 4, 5, 6, 7)] int readingEnsurance)
        {
            Assume.That(maxNumberLength + maxStingLength + 3 <
                        bufferSize - readingEnsurance);

            var linesIndexes = new LineIndexes[linesCount];
            var linesStorageMock = new Mock<ILinesIndexesStorage>();

            linesStorageMock
                .SetupGet(o => o.Length)
                .Returns(linesCount);

            linesStorageMock
                .Setup(o => o.Indexes)
                .Returns(linesIndexes);

            var configMock = new Mock<IConfig>();
            configMock
                .SetupGet(o => o.BufferSize)
                .Returns(bufferSize);
            configMock
                .SetupGet(o => o.GroupBufferRowReadingEnsurance)
                .Returns(readingEnsurance);

            IBuffersPool buffersPool =
                new InfinityBuffersPool(bufferSize);

            IGroupsService groupMatrixService =
                new GroupsService(buffersPool, configMock.Object);

            ISortedGroupWriter sortedGroupWriter =
                new SortedGroupWriterMaker(linesStorageMock.Object);

            ILinesIndexesExtractor linesIndexesExtractor =
                new LinesIndexesExtractor(linesStorageMock.Object);

            var lines = GroupLinesGenerator
                .Generate("ox", linesCount, maxNumberLength, maxStingLength)
                .ToArray();

            var groupStream = new MemoryStream();
            foreach (var line in lines)
                groupStream.Write(line, 0, line.Length);
            groupStream.Position = 0;

            var groupReader = new MemoryReader(groupStream);
            var groupInfo = new GroupInfo
            {
                BytesCount = (int) groupStream.Length,
                LinesCount = linesCount,
                Mapping = new[] {new LongRange(0, (int)groupStream.Length)}
            };

            IGroup matrix;
            Assert.IsTrue(groupMatrixService.TryCreateGroup(groupInfo, out matrix));

            groupMatrixService.LoadGroup(matrix, groupInfo, groupReader);
            linesIndexesExtractor.ExtractIndexes(matrix, new Range(0, linesCount));

            var indexedLines = Enumerable
                .Zip(lines, linesIndexes, 
                    (line, indexes) => new {line, indexes})
                .ToArray();

            Mix(indexedLines, linesCount / 2);

            var expectedMixedGroup = new byte[groupStream.Length];
            var expectedMixedGroupStream = new MemoryStream(
                expectedMixedGroup);

            for (int i = 0; i < linesCount; i++)
            {
                var line = indexedLines[i].line;
                expectedMixedGroupStream
                    .Write(line, Consts.EndLineBytesCount,
                        line.Length - Consts.EndLineBytesCount);

                expectedMixedGroupStream
                    .Write(Consts.EndLineBytes, 0, Consts.EndLineBytesCount);
            }

            linesIndexes = indexedLines
                .Select(o => o.indexes)
                .ToArray();
            
            linesStorageMock
                .Setup(o => o.Indexes)
                .Returns(linesIndexes);
            
            var mixedGroupStream = new MemoryStream((int) groupStream.Length);
            var mixedGroupWriter = new MemoryWriter(mixedGroupStream);

            sortedGroupWriter.Write(matrix, 
                new Contracts.Range(0, linesCount),
                mixedGroupWriter);

            var mixedGroupContent = mixedGroupStream.ToArray();

            CollectionAssert.AreEqual(
                expectedMixedGroup,
                mixedGroupContent);
        }
    }
}
