using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bigsort.Contracts;
using Bigsort.Implementation;
using Moq;
using static Bigsort.Tests.Tools;
using NUnit.Framework;

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
            [Values(8, 9, 10, 11, 12, 13, 14, 15, 16, 1024, 32 * 1024)] int bufferSize,
            [Values(0, 1, 2, 3, 4, 5, 6, 7)] int readingEnsurance)
        {
            Assume.That(maxNumberLength + maxStingLength + 3 <
                        bufferSize - readingEnsurance);
            
            var linesIndexes = new LineIndexes[linesCount];
            var linesSorageMock = new Mock<ILinesIndexesStorage>();

            linesSorageMock
                .SetupGet(o => o.Length)
                .Returns(linesCount);

            linesSorageMock
                .Setup(o => o.Indexes)
                .Returns(linesIndexes);

            var configMock = new Mock<IConfig>();
            configMock
                .SetupGet(o => o.BufferSize)
                .Returns(bufferSize);
            configMock
                .SetupGet(o => o.GroupBufferRowReadingEnsurance)
                .Returns(readingEnsurance);

            IUsingHandleMaker usingHandleMaker =
                new UsingHandleMaker();

            IPoolMaker poolMaker =
                new PoolMaker(usingHandleMaker);

            IBuffersPool buffersPool =
                new BuffersPool(poolMaker, configMock.Object);

            IGroupBytesMatrixService groupBytesMatrixService =
                new GroupBytesMatrixService(buffersPool, configMock.Object);

            ISortedGroupWriter sortedGroupWriter = 
                new SortedGroupWriter(linesSorageMock.Object);

            ILinesIndexesExtractor linesIndexesExtractor = 
                new LinesIndexesExtractor(linesSorageMock.Object);

            var lines = GroupLinesGenerator
                .Generate("ox", linesCount, maxNumberLength, maxStingLength)
                .ToArray();
            
            var groupStream = new MemoryStream();
            foreach (var line in lines)
                groupStream.Write(line, 0, line.Length);
            groupStream.Position = 0;

            var groupReader = new MemoryReader(groupStream);
            var groupInfoMock = new Mock<IGroupInfo>();

            groupInfoMock
                .SetupGet(o => o.BytesCount)
                .Returns((int) groupStream.Length);
            groupInfoMock
                .SetupGet(o => o.LinesCount)
                .Returns(linesCount);
            groupInfoMock
                .SetupGet(o => o.Mapping)
                .Returns(new[] {new LongRange(0, groupStream.Length) });
            
            var rowInfo = groupBytesMatrixService
                .CalculateRowsInfo((int) groupStream.Length);

            var group = groupBytesMatrixService
                .LoadMatrix(rowInfo, groupInfoMock.Object, groupReader);

            linesIndexesExtractor
                .ExtractIndexes(group, new Contracts.Range(0, linesCount));

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
            
            linesSorageMock
                .Setup(o => o.Indexes)
                .Returns(linesIndexes);
            
            var mixedGroupStream = new MemoryStream((int) groupStream.Length);
            var mixedGroupWriter = new MemoryWriter(mixedGroupStream);

            sortedGroupWriter.Write(group, 
                new Contracts.Range(0, linesCount),
                mixedGroupWriter);

            var mixedGroupContent = mixedGroupStream.ToArray();

            CollectionAssert.AreEqual(
                expectedMixedGroup,
                mixedGroupContent);
        }
    }
}
