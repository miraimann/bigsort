using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bigsort.Contracts;
using Bigsort.Implementation;
using Moq;
using NUnit.Framework;
using static Bigsort.Tests.Tools;

namespace Bigsort.Tests
{
    [TestFixture]
    public class GroupMatrixServiceTests
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
                .SetupGet(o => o.PhysicalBufferLength)
                .Returns(bufferSize);
            configMock
                .SetupGet(o => o.BufferReadingEnsurance)
                .Returns(readingEnsurance);

            var buffersPool = new InfinityBuffersPool(bufferSize);

            IGroupsService service = 
                new GroupsService(buffersPool, configMock.Object);

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
            var reader = new MemoryReader(inputStream);

            var linesCount = random.Next(0, int.MaxValue);
            var bytesCount = (int)blocks.Value.Sum(o => o.Length);

            var groupInfo = new GroupInfo
            {
                BytesCount = bytesCount,
                LinesCount = linesCount,
                Mapping = blocks.Value
            };

            var expectedRowLength = bufferSize - readingEnsurance;
            var expectedRowsCount = (bytesCount / expectedRowLength)
                                  + (bytesCount % expectedRowLength == 0 ? 0 : 1);

            IGroup matrix;

            Assert.IsTrue(service.TryCreateGroup(groupInfo, out matrix));

            Assert.AreEqual(expectedRowLength, matrix.RowLength);
            Assert.AreEqual(expectedRowsCount, matrix.BuffersCount);
            Assert.AreEqual(expectedRowsCount, matrix.Buffers.Length);
            Assert.AreEqual(linesCount, matrix.LinesCount);
            Assert.AreEqual(bytesCount, matrix.BytesCount);

            service.LoadGroup(matrix, groupInfo, reader);

            Assert.AreEqual(expectedRowLength, matrix.RowLength);
            Assert.AreEqual(expectedRowsCount, matrix.BuffersCount);
            Assert.AreEqual(expectedRowsCount, matrix.Buffers.Length);
            Assert.AreEqual(linesCount, matrix.LinesCount);
            Assert.AreEqual(bytesCount, matrix.BytesCount);
            
            var expectedGroupBytes =
                blocks.Value
                      .Select(block => input.Skip((int) block.Offset)
                                            .Take((int) block.Length))
                      .Aggregate(Enumerable.Concat)
                      .ToArray();
#region DEBUG
// #if DEBUG
//             var expectedGroupBytesInLine = 
//                 string.Join(", ", expectedGroupBytes.Select(o => $"{o:000}"));
//             
//             var actualGroupBytesInLine =
//                 string.Join(", ", matrix.Select(o => $"{o:000}"));
// #endif
#endregion
            CollectionAssert.AreEqual(expectedGroupBytes, matrix);
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
