using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bigsort.Contracts;
using Bigsort.Implementation;
using Moq;
using NUnit.Framework;
using static Bigsort.Tests.Tools;

namespace Bigsort.Tests
{
    [TestFixture]
    public partial class GroupSorterTests
    {
        private const int
            BufferReadingEnsurance = 7,
            LinesStorageLength = 1024,
            LargeBufferSize = 1024,
            LinesRangeOffset = 12,
            BuffersOffset = 9,
            OverBuffersCount = 13;

        [Test]
        [Parallelizable]
        [Timeout(10000)]
        public void Test(
            [ValueSource(nameof(TestCases))] TestCase testCase,
            [ValueSource(nameof(BufferSizes))] BufferSize bufferSize)
        {
            var configMock = new Mock<IConfig>();

            int usingBufferSize = LargeBufferSize;
            if (bufferSize < BufferSize.Large)
            {
                usingBufferSize = sizeof(ulong)
                                + BufferReadingEnsurance;
                if (bufferSize != BufferSize.Min)
                    ++usingBufferSize;
            }
            
            configMock
                .SetupGet(o => o.UsingBufferLength)
                .Returns(usingBufferSize);
            configMock
                .SetupGet(o => o.BufferReadingEnsurance)
                .Returns(BufferReadingEnsurance);

            var physicalBufferLength =
                usingBufferSize + BufferReadingEnsurance;

            configMock
                .SetupGet(o => o.PhysicalBufferLength)
                .Returns(physicalBufferLength);
            
            ISortingSegmentsSupplier sortingSegmentsSupplier =
                new SortingSegmentsSupplier(
                    configMock.Object);

            var lineIndexesExtractorMock =
                new Mock<ILinesIndexesExtractor>();

            IGroupSorter groupSorter = 
                new GroupSorter(
                    sortingSegmentsSupplier,
                    lineIndexesExtractorMock.Object);

            var groupBytesCount = testCase.GroupBytes.Length;            
            var groupBuffersCount = (int) Math.Ceiling((double)
                groupBytesCount / usingBufferSize);

            var buffersCount = BuffersOffset 
                             + groupBuffersCount 
                             + OverBuffersCount;

            byte[][] buffers = Enumerable
                .Range(0, buffersCount)
                .Select(_ => new byte[physicalBufferLength])
                .ToArray();

            for (int i = 0; i < groupBuffersCount - 1; i++)
                Array.Copy(testCase.GroupBytes, i * usingBufferSize,
                           buffers[BuffersOffset + i], 0,
                           usingBufferSize);

            Array.Copy(testCase.GroupBytes, (groupBuffersCount - 1) * usingBufferSize,
                       buffers[BuffersOffset + groupBuffersCount - 1], 0,
                       groupBytesCount % usingBufferSize);

            var lines = new LineIndexes[LinesStorageLength];
            var segments = new ulong[LinesStorageLength];

            var groupMock = new Mock<IGroup>();
            groupMock
                .SetupGet(o => o.BytesCount)
                .Returns(groupBytesCount);

            groupMock
                .SetupGet(o => o.Buffers)
                .Returns(new ArraySegment<byte[]>(
                                buffers, 
                                BuffersOffset, 
                                groupBuffersCount));
            groupMock
                .SetupGet(o => o.Lines)
                .Returns(new ArraySegment<LineIndexes>(
                                lines,
                                LinesRangeOffset,
                                testCase.InputLines.Length));
            groupMock
                .SetupGet(o => o.SortingSegments)
                .Returns(new ArraySegment<ulong>(
                                segments,
                                LinesRangeOffset,
                                testCase.InputLines.Length));

            lineIndexesExtractorMock
                .Setup(o => o.ExtractIndexes(groupMock.Object))
                .Callback(() => 
                                Array.Copy(testCase.InputLines, 0,
                                           lines, LinesRangeOffset,
                                           testCase.InputLines.Length));

            groupSorter.Sort(groupMock.Object);

            var resultLines = lines
                .Skip(LinesRangeOffset)
                .Take(testCase.InputLines.Length)
                .Select(o => o.start)
                .ToArray();

            Console.WriteLine(string.Join(" ", testCase.ExpectedSortedLines));
            Console.WriteLine(string.Join(" ", resultLines));

            CollectionAssert.AreEqual(
                testCase.ExpectedSortedLines,
                resultLines);
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
