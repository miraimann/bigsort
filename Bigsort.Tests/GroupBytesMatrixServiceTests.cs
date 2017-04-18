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
    public class GroupBytesMatrixServiceTests
    {
        [Test]
        [Timeout(10000)]
        public void Test(
            [ValueSource(nameof(Blocks_s))] LongRanges blocks,
            [ValueSource(nameof(BufferSizes))] int bufferSize,
            [ValueSource(nameof(ReadingEnsurances))] int readingEnsurance)
        {
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

            IGroupBytesMatrixService service = 
                new GroupBytesMatrixService(buffersPool, configMock.Object);

            var lastBlock = blocks.Value[blocks.Value.Length - 1];
            var inputSize = lastBlock.Offset + lastBlock.Length;
            var input = new byte[inputSize];

            var random = new Random();
            foreach (var block in blocks.Value)
            {
                var blockContent = new byte[block.Length];
                random.NextBytes(blockContent);
                Array.Copy(blockContent, 0, 
                           input, block.Offset, 
                           block.Length);
            }

            var inputStream = new MemoryStream(input);
            var fileReaderMock = new Mock<IFileReader>();
            fileReaderMock
                .SetupGet(o => o.Length)
                .Returns(inputSize);
            fileReaderMock
                .SetupGet(o => o.Position)
                .Returns(() => inputStream.Position);
            fileReaderMock
                .SetupSet(o => o.Position = It.IsAny<long>())
                .Callback((long value) => inputStream.Position = value);
            fileReaderMock
                .Setup(o => o.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((byte[] buff, int offset, int length) => 
                                inputStream.Read(buff, offset, length));

            var linesCount = random.Next(0, int.MaxValue);
            var bytesCount = (int)blocks.Value.Sum(o => o.Length);

            var groupInfoMock = new Mock<IGroupInfo>();
            groupInfoMock
                .SetupGet(o => o.BytesCount)
                .Returns(bytesCount);
            groupInfoMock
                .SetupGet(o => o.LinesCount)
                .Returns(linesCount);
            groupInfoMock
                .SetupGet(o => o.Mapping)
                .Returns(blocks.Value);

            var expectedRowLength = bufferSize - readingEnsurance;
            var expectedRowsCount = (bytesCount / expectedRowLength)
                                  + (bytesCount % expectedRowLength == 0 ? 0 : 1);

            var rowInfo = service.CalculateRowsInfo(bytesCount);
            
            Assert.AreEqual(expectedRowLength, rowInfo.RowLength);
            Assert.AreEqual(expectedRowsCount, rowInfo.RowsCount);
            
            var group = service.LoadMatrix(
                rowInfo,
                groupInfoMock.Object,
                fileReaderMock.Object);

            Assert.AreEqual(expectedRowLength, group.RowLength);
            Assert.AreEqual(expectedRowsCount, group.RowsCount);
            Assert.AreEqual(expectedRowsCount, group.Rows.Length);
            Assert.AreEqual(linesCount, group.LinesCount);
            Assert.AreEqual(bytesCount, group.BytesCount);
            
            var expectedGroupBytes =
                blocks.Value
                      .Select(block => input.Skip((int) block.Offset)
                                            .Take((int) block.Length))
                      .Aggregate(Enumerable.Concat)
                      .ToArray();
#region DEBUG
#if DEBUG
            var expectedGroupBytesInLine = 
                string.Join(", ", expectedGroupBytes.Select(o => $"{o:000}"));
            
            var actualGroupBytesInLine =
                string.Join(", ", group.Select(o => $"{o:000}"));
#endif
#endregion
            CollectionAssert.AreEqual(expectedGroupBytes, group);
        }

        public static int[] BufferSizes = Enumerable.Range(8, 16).ToArray();
        public static int[] ReadingEnsurances = Enumerable.Range(0, 8).ToArray();


        public class LongRanges
        {
            private readonly string _name;

            public LongRanges(string name, LongRange[] value)
            {
                Value = value;
                _name = name;
            }
            
            public LongRange[] Value { get; }

            public override string ToString() =>
                _name;
        }

        public static IEnumerable<LongRanges> Blocks_s
        {
            get
            {
                yield return new LongRanges("00", new[]
                {
                    new LongRange(0, 1),
                    new LongRange(1, 1)
                });

                yield return new LongRanges("01", new[]
                {
                    new LongRange(0, 1024)
                });

                yield return new LongRanges("02", new[]
                {
                    new LongRange(37, 1024)
                });

                yield return new LongRanges("03", new[]
                {
                    new LongRange(37, 1000),
                    new LongRange(1037, 1000)
                });

                yield return new LongRanges("04", new[]
                {
                    new LongRange(7, 8),
                    new LongRange(29, 8),
                    new LongRange(137, 8),
                    new LongRange(237, 8),
                    new LongRange(1037, 8),
                    new LongRange(2037, 8)
                });

                yield return new LongRanges("05", new[]
                {
                    new LongRange(7, 14),
                    new LongRange(29, 14),
                    new LongRange(137, 14),
                    new LongRange(237, 14),
                    new LongRange(1037, 14),
                    new LongRange(2037, 14)
                });
            }
        }
    }
}
