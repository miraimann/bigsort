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
    public class GroupsLoaderTests
    {
        [Test]
        [Parallelizable]
        [Timeout(10000)]
        public void SingleGroupTest(
            [ValueSource(nameof(Blocks_s))] LongRanges blocks,
            [ValueSource(nameof(BufferSizes))] int usingBufferSize)
        {
            Assume.That(usingBufferSize >= blocks.Value.Max(o => o.Length));

            const string groupsFilePath = "ZZZZzzzzZZZZzzzzZZZ";
            const int buffersMemoryLimit = 1024 * 1024;

            var configMock = new Mock<IConfig>();
            var physicalBufferLength = usingBufferSize + sizeof(ulong);
            configMock
                .SetupGet(o => o.PhysicalBufferLength)
                .Returns(physicalBufferLength);
            configMock
                .SetupGet(o => o.UsingBufferLength)
                .Returns(usingBufferSize);
            configMock
                .SetupGet(o => o.BufferReadingEnsurance)
                .Returns(sizeof(ulong));
            
            var bytesCount = blocks.Value.Sum(o => o.Length);
            var linesCount = bytesCount / 4; // max

            var buffersPool = new InfinityBuffersPool(
                physicalBufferLength,
                buffersMemoryLimit);

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
            
            var ioServiceMock = new Mock<IIoService>();
            ioServiceMock
                .Setup(o => o.OpenRead(groupsFilePath, 0L))
                .Returns(() => new MemoryReader(new MemoryStream(input)));
            
            IGroupsLoaderMaker loaderMaker =
                new GroupsLoaderMaker(
                    groupsFilePath,
                    buffersPool,
                    ioServiceMock.Object,
                    configMock.Object);
            
            var groupIndex = random.Next(Consts.MaxGroupsCount);
            var infos = new GroupInfo[Consts.MaxGroupsCount];
            var groups = new IGroup[Consts.MaxGroupsCount];
             
            infos[groupIndex] = new GroupInfo
            {
                BytesCount = bytesCount,
                LinesCount = linesCount,
                Mapping = blocks.Value
            };

            var loader = loaderMaker.Make(infos, groups);
            var range = loader.LoadNextGroups();

            Assert.AreEqual(0, range.Offset);
            Assert.AreEqual(Consts.MaxGroupsCount, range.Length);

            Assert.IsTrue(infos
                .Where((_, i) => i != groupIndex)
                .All(GroupInfo.IsZero));

            Assert.IsTrue(groups
                .Where((_, i) => i != groupIndex)
                .All(o => o == null));

            var actualGroup = groups[groupIndex];

            Assert.IsNotNull(actualGroup);
            Assert.AreEqual(0, actualGroup.Lines.Offset);
            Assert.AreEqual(linesCount, actualGroup.Lines.Count);
            Assert.AreEqual(0, actualGroup.SortingSegments.Offset);
            Assert.AreEqual(linesCount, actualGroup.SortingSegments.Count);
            Assert.GreaterOrEqual(actualGroup.Lines.Array.Length, linesCount);
            Assert.GreaterOrEqual(actualGroup.SortingSegments.Array.Length, linesCount);
            
            var actualGroupBytes = new byte[actualGroup.BytesCount];
            var buffers = actualGroup.Buffers;
            int j = 0;
            for (; j < buffers.Count - 1; j++)
                Array.Copy(buffers.Array[buffers.Offset + j], 0,
                           actualGroupBytes, j * usingBufferSize,
                           usingBufferSize);

            Array.Copy(buffers.Array[buffers.Offset + j], 0,
                       actualGroupBytes, j * usingBufferSize,
                       actualGroup.BytesCount - j * usingBufferSize);

            var expectedGroupBytes = blocks.Value
                .Select(block => input.Skip((int)block.Offset)
                                      .Take(block.Length))
                .Aggregate(Enumerable.Concat)
                .ToArray();
            
            Console.WriteLine(string.Join(" ", expectedGroupBytes));
            Console.WriteLine(string.Join(" ", actualGroupBytes));

            CollectionAssert.AreEqual(
                expectedGroupBytes, 
                actualGroupBytes);
        }

        public static int[]
            BufferSizes = Enumerable.Concat(
                    Enumerable.Range(8, 16),
                    new[] {1024, 1025})
                .ToArray();

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
                    new LongRange(0, 4),
                    new LongRange(4, 4)
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

                yield return new LongRanges("06", new[]
{
                    new LongRange(7, 7),
                    new LongRange(29, 8),
                    new LongRange(137, 9),
                    new LongRange(237, 2),
                    new LongRange(1037, 3),
                    new LongRange(2037, 2)
                });
            }
        }
    }
}
