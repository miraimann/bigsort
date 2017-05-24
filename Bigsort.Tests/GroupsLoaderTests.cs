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
            var physicalBufferLength = usingBufferSize 
                                     + Consts.BufferReadingEnsurance;
            configMock
                .SetupGet(o => o.PhysicalBufferLength)
                .Returns(physicalBufferLength);
            configMock
                .SetupGet(o => o.UsingBufferLength)
                .Returns(usingBufferSize);
            configMock
                .SetupGet(o => o.GroupsFilePath)
                .Returns(groupsFilePath);
            
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
            
            IGroupsLoaderFactory loaderMaker =
                new GroupsLoaderFactory(
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

            var loader = loaderMaker.Create(infos, groups);
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
            
            // Console.WriteLine(string.Join(" ", expectedGroupBytes));
            // Console.WriteLine(string.Join(" ", actualGroupBytes));
            
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

            public LongRanges(string name, 
                long[] offsets, 
                int[] lengths)
            {
                _name = name;
                Value = Enumerable
                    .Zip(offsets, lengths, (a, b) => new LongRange(a, b))
                    .ToArray();
            }

            internal LongRange[] Value { get; }

            public override string ToString() =>
                _name;
        }

        public static IEnumerable<LongRanges> Blocks_s
        {
            get
            {
                yield return new LongRanges("00", 
                    new long[] {0, 4},
                    new int [] {4, 4});

                yield return new LongRanges("01",
                    new long[] {0},
                    new int[] {1024});

                yield return new LongRanges("02",
                    new long[] {37},
                    new int[] {1024});

                yield return new LongRanges("03",
                    new long[] { 37, 1037 },
                    new int[] { 1000, 1000 });

                yield return new LongRanges("04",
                    new long[] { 7, 29, 137, 237, 1037, 2037 },
                    new int[]  { 8,  8,   8,   8,    8,    8 });

                yield return new LongRanges("05",
                    new long[] {  7, 29, 137, 237, 1037, 2037 },
                    new int[]  { 14, 14,  14,  14,   14,   14 });

                yield return new LongRanges("06",
                    new long[] { 7, 29, 137, 237, 1037, 2037 },
                    new int[]  { 7,  8,   9,   2,    3,    2 });
            }
        }
    }
}
