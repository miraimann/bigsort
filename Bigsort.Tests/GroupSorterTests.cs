using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bigsort.Contracts;
using Bigsort.Implementation;
using Moq;
using NUnit.Framework;
using static Bigsort.Tests.Tools;
using Range = Bigsort.Contracts.Range;

namespace Bigsort.Tests
{
    [TestFixture]
    public partial class GroupSorterTests
    {
        private const int
            GroupBufferRowReadingEnsurance = 7,
            LinesStorageLength = 1024,
            LargeBufferSize = 1024,
            LinesRangeOffset = 12;

        private class Setup<T>
            where T : IEquatable<T>
                    , IComparable<T>
        {
            public readonly Mocks MockOf;
            public readonly LineIndexes[] StorageLineIndexes;
            public readonly T[] Segments;
            public readonly MemoryStream GroupsFileStream;
            public readonly ISegmentService<T> SegmentService;
            public readonly ISortingSegmentsSupplier SortingSegmentsSupplier;
            
            public readonly IGroupSorter GroupSorter;

            public IBuffersPool BuffersPool;
            public LineIndexes[] LineIndexes;
            public IGroupsService GroupMatrixService;
            public IGroup Group;

            public Setup(ISegmentService<T> segmentService)
            {
                StorageLineIndexes = new LineIndexes[LinesStorageLength];
                Segments = new T[LinesStorageLength];

                MockOf = new Mocks();

                MockOf.LinesStorage = new Mock<ILinesStorage<T>>();
                MockOf.LinesStorage
                      .SetupGet(o => o.Length)
                      .Returns(LinesStorageLength);

                MockOf.LinesStorage
                      .SetupGet(o => o.Segments)
                      .Returns(Segments);

                MockOf.LinesStorage
                      .SetupGet(o => o.Indexes)
                      .Returns(StorageLineIndexes);

                MockOf.LinesIndexesExtructor = new Mock<ILinesIndexesExtractor>();
                MockOf.LinesIndexesExtructor
                      .Setup(o => o.ExtractIndexes(
                          It.IsAny<IFixedSizeList<byte>>(),
                          It.IsAny<Range>()))
                      .Callback((IFixedSizeList<byte> groupBytes, Range linesRange) =>
                          Array.Copy(LineIndexes, 0,
                                     StorageLineIndexes,
                                     linesRange.Offset,
                                     linesRange.Length));

                GroupsFileStream = new MemoryStream();
                MockOf.Config = new Mock<IConfig>();
                MockOf.Config
                      .SetupGet(o => o.GroupBufferRowReadingEnsurance)
                      .Returns(GroupBufferRowReadingEnsurance);
                
                SegmentService = segmentService;
                SortingSegmentsSupplier = new SortingSegmentsSupplier<T>(
                    MockOf.LinesStorage.Object,
                    SegmentService);

                GroupSorter = new GroupSorter<T>(
                    SortingSegmentsSupplier,
                    MockOf.LinesIndexesExtructor.Object,
                    MockOf.LinesStorage.Object,
                    SegmentService);
            }

            public void SetupCase(TestCase testCase,
                                  BufferSize bufferSize)
            {
                int buffSize = LargeBufferSize;
                if (bufferSize < BufferSize.Large)
                {
                    buffSize = SegmentService.SegmentSize
                             + GroupBufferRowReadingEnsurance;
                    if (bufferSize != BufferSize.Min)
                        ++buffSize;
                }

                MockOf.Config
                      .SetupGet(o => o.BufferSize)
                      .Returns(buffSize);

                BuffersPool = new InfinityBuffersPool(buffSize);
                GroupMatrixService = new GroupsService(
                       BuffersPool,
                       MockOf.Config.Object);

                var bytesCount = testCase.GroupBytes.Length;
                var linesCount = testCase.InputLines.Length;

                var groupInfo = new GroupInfo
                {
                    LinesCount = linesCount,
                    BytesCount = bytesCount,
                    Mapping = new[] {new LongRange(0, bytesCount)}
                };

                GroupsFileStream.Write(testCase.GroupBytes, 0,
                                       testCase.GroupBytes.Length);
                GroupsFileStream.Position = 0;

                LineIndexes = testCase.InputLines;

                Group = GroupMatrixService.TryCreateGroup(groupInfo);
                Assert.IsNotNull(Group);
                GroupMatrixService.LoadGroup(Group, groupInfo, 
                    new MemoryReader(GroupsFileStream));
            }

            public class Mocks
            {
                public Mock<IConfig> Config;
                public Mock<ILinesIndexesExtractor> LinesIndexesExtructor;
                public Mock<ILinesStorage<T>> LinesStorage;
            }
        }

        [Test]
        [Parallelizable]
        [Timeout(10000)]
        public void ByteSegmentTest(
                [ValueSource(nameof(TestCases))] TestCase testCase,
                [ValueSource(nameof(BufferSizes))] BufferSize bufferSize) =>
            Test(new Setup<byte>(new ByteSegmentService()), testCase, bufferSize);

        [Test]
        [Parallelizable]
        [Timeout(10000)]
        public void UInt32SegmentTest(
                [ValueSource(nameof(TestCases))] TestCase testCase,
                [ValueSource(nameof(BufferSizes))] BufferSize bufferSize) =>
            Test(new Setup<uint>(new UInt32SegmentService()), testCase, bufferSize);

        [Test]
        [Parallelizable]
        [Timeout(10000)]
        public void UInt64SegmentTest(
                [ValueSource(nameof(TestCases))] TestCase testCase,
                [ValueSource(nameof(BufferSizes))] BufferSize bufferSize) =>
            Test(new Setup<ulong>(new UInt64SegmentService()), testCase, bufferSize);

        private void Test<T>(Setup<T> setup,
            TestCase testCase, BufferSize bufferSize)
                where T : IEquatable<T>, IComparable<T>
        {
            setup.SetupCase(testCase, bufferSize);

            setup.GroupSorter.Sort(setup.Group,
                new Range(LinesRangeOffset, testCase.InputLines.Length));

            var resultLines = new LineIndexes[testCase.InputLines.Length];
            Array.Copy(setup.StorageLineIndexes, LinesRangeOffset,
                       resultLines, 0, testCase.InputLines.Length);

            Assert.AreEqual(
                testCase.ExpectedSortedLines,
                resultLines.Select(o => o.start)
                );
        }

        public static IEnumerable<TestCase> TestCases =>
            new[] { Cases_00_09, Cases_10_19, Cases_20_29 }
                .Aggregate(Enumerable.Concat);

        public class TestCase
        {
            private readonly string _name;

            public TestCase(string name,
                InputLineList input, int[] expected)
            {
                _name = name;

                InputLines = input.CombineLinesIndexes();
                GroupBytes = input.CombineLinesBytes();
                ExpectedSortedLines = expected;
                BytesView = input.BytesView;
            }

            public LineIndexes[] InputLines { get; }
            public byte[] GroupBytes { get; }
            public int[] ExpectedSortedLines { get; }

            public string BytesView { get; }

            public override string ToString() =>
                _name;

            public class InputLineList
                : IEnumerable<InputLineList.Item>
            {
                private readonly IList<Item> _items =
                    new List<Item>();

                public void Add(
                    string indexes,   // start|letters count|digits count
                    string bytesView) // xxnumber.string
                {
                    BytesView += Environment.NewLine;
                    BytesView += bytesView;
                    bytesView = bytesView.Replace(" ", string.Empty);

                    if (bytesView[1] == '[')
                    {
                        var processedBytesView =
                            new StringBuilder(bytesView.Length);

                        var digitsCountRaw = bytesView.Skip(2)
                            .TakeWhile(c => c != ']')
                            .Aggregate(string.Empty, (acc, c) => acc + c);

                        bytesView = processedBytesView
                            .Append(bytesView[0])
                            .Append((char)byte.Parse(digitsCountRaw))
                            .Append(bytesView.Substring(digitsCountRaw.Length + 3))
                            .ToString();
                    }

                    var lineIndexes = LineIndexes.Parse(indexes);
                    var lineBytes = BytesOfString(bytesView);
                    lineBytes[0] = (byte)'\r';
                    lineBytes[1] = lineIndexes.digitsCount;

                    _items.Add(new Item(lineIndexes, lineBytes));
                }

                public string BytesView { get; private set; }

                public byte[] CombineLinesBytes() =>
                    _items.SelectMany(o => o.Bytes)
                          .ToArray();

                public LineIndexes[] CombineLinesIndexes() =>
                    _items.Select(o => o.Indexes)
                          .ToArray();

                public IEnumerator<Item> GetEnumerator() =>
                    _items.GetEnumerator();

                IEnumerator IEnumerable.GetEnumerator() =>
                    GetEnumerator();

                public class Item
                {
                    public Item(LineIndexes indexes,
                        byte[] bytes)
                    {
                        Indexes = indexes;
                        Bytes = bytes;
                    }

                    public LineIndexes Indexes { get; }
                    public byte[] Bytes { get; }

                }
            }
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
    }
}
