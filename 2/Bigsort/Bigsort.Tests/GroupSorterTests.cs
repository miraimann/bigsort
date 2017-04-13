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
        private const string GroupFilePath = "bdpqbqpdqbpppdqqbbpppqp";

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
            public readonly MemoryStream GroupStream;
            public readonly IBuffersPool BuffersPool;
            public readonly IUsingHandleMaker DisposableValueMaker;
            public readonly ISegmentService<T> SegmentService;
            public readonly ISortingSegmentsSupplier SortingSegmentsSupplier;
            
            public readonly IGroupSorter GroupSorter;

            public LineIndexes[] LineIndexes;
            public IGroupBytesMatrixService GroupBytesLoader;
            public IGroupBytesMatrix Group;

            public Setup(ISegmentService<T> segmentService, 
                bool forIntegration = false)
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

                GroupStream = new MemoryStream(1024);
                MockOf.GroupReader = new Mock<IFileReader>();
                MockOf.GroupReader
                      .Setup(o => o.Read(
                          It.IsAny<byte[]>(),
                          It.IsAny<int>(),
                          It.IsAny<int>()))
                      .Returns((byte[] buff, int offset, int count) =>
                                GroupStream.Read(buff, offset, count));

                MockOf.IoService = new Mock<IIoService>();
                MockOf.IoService
                      .Setup(o => o.OpenRead(GroupFilePath))
                      .Returns(MockOf.GroupReader.Object);

                MockOf.GroupInfo = new Mock<IGroupInfo>();
                MockOf.GroupInfo
                      .SetupGet(o => o.Name)
                      .Returns(GroupFilePath);

                MockOf.Config = new Mock<IConfig>();
                MockOf.Config
                      .SetupGet(o => o.IsLittleEndian)
                      .Returns(BitConverter.IsLittleEndian);

                MockOf.Config
                      .SetupGet(o => o.GroupBufferRowReadingEnsurance)
                      .Returns(GroupBufferRowReadingEnsurance);

                DisposableValueMaker = new UsingHandleMaker();
                BuffersPool = new BuffersPool(
                    DisposableValueMaker,
                    MockOf.Config.Object);

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

                GroupBytesLoader = new GroupBytesLoader(
                       BuffersPool,
                       MockOf.IoService.Object,
                       MockOf.Config.Object);

                MockOf.GroupInfo
                      .SetupGet(o => o.LinesCount)
                      .Returns(testCase.InputLines.Length);

                MockOf.GroupInfo
                      .SetupGet(o => o.BytesCount)
                      .Returns(testCase.GroupBytes.Length);

                GroupStream.Write(testCase.GroupBytes, 0,
                       testCase.GroupBytes.Length);
                GroupStream.Position = 0;

                LineIndexes = testCase.InputLines;

                var groupInfo = GroupBytesLoader
                      .CalculateMatrixInfo(MockOf.GroupInfo.Object);
                Group = GroupBytesLoader
                      .LoadMatrix(groupInfo);
            }

            public class Mocks
            {
                public Mock<IConfig> Config;
                public Mock<IFileReader> GroupReader;
                public Mock<IIoService> IoService;
                public Mock<ILinesIndexesExtractor> LinesIndexesExtructor;
                public Mock<IGroupInfo> GroupInfo;
                public Mock<ILinesStorage<T>> LinesStorage;
            }
        }

        [Test]
        [Timeout(10000)]
        public void ByteSegmentTest(
                [ValueSource(nameof(TestCases))] TestCase testCase,
                [ValueSource(nameof(BufferSizes))] BufferSize bufferSize) =>
            Test(new Setup<byte>(new ByteSegmentService()), testCase, bufferSize);
       
        [Test]
        [Timeout(10000)]
        public void UInt32SegmentTest(
                [ValueSource(nameof(TestCases))] TestCase testCase,
                [ValueSource(nameof(BufferSizes))] BufferSize bufferSize) =>
            Test(new Setup<uint>(new UInt32SegmentService(BitConverter.IsLittleEndian)),
                testCase, bufferSize);

        [Test]
        [Timeout(10000)]
        public void UInt64SegmentTest(
                [ValueSource(nameof(TestCases))] TestCase testCase,
                [ValueSource(nameof(BufferSizes))] BufferSize bufferSize) =>
            Test(new Setup<ulong>(new UInt64SegmentService(BitConverter.IsLittleEndian)),
                 testCase, bufferSize);

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
            new[] {Cases_00_09, Cases_10_19, Cases_20_29}
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
                    lineBytes[0] = (byte) '\r';
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
