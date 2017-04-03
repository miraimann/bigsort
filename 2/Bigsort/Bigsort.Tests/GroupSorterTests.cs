using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            LinesStorageLength = 1024,
            LinesRangeOffset = 12;

        private LineIndexes[] _storageLineIndexes, _lineIndexes;
        private byte[] _byteSegments;

        private Mock<IConfig> _configMock;
        private Mock<IReader> _groupReader;
        private Mock<IIoService> _ioServiceMock;
        private Mock<ILinesIndexesExtractor> _linesIndexesExtructor;
        private Mock<IGroupInfo> _groupInfoMock;
        private Mock<ILinesStorage<byte>> _linesStorageMock;

        private MemoryStream _groupStream;

        private IGroupBytesLoader _groupBytesLoader;
        private IBuffersPool _buffersPool;

        private IDisposableValueMaker _disposableValueMaker;
        private ISegmentService<byte> _byteSegmentService;
        private ISortingSegmentsSupplier _sortingSegmentsSupplier;
        private IGroupSorter _groupSorter;

        [SetUp]
        public void Setup()
        {
            _storageLineIndexes = new LineIndexes[LinesStorageLength];
            _byteSegments = new byte[LinesStorageLength];

            _linesStorageMock = new Mock<ILinesStorage<byte>>();
            _linesStorageMock
                .SetupGet(o => o.Length)
                .Returns(LinesStorageLength);

            _linesStorageMock
                .SetupGet(o => o.Segments)
                .Returns(_byteSegments);

            _linesStorageMock
                .SetupGet(o => o.Indexes)
                .Returns(_storageLineIndexes);

            _linesIndexesExtructor = new Mock<ILinesIndexesExtractor>();
            _linesIndexesExtructor
                .Setup(o => o.ExtractIndexes(
                    It.IsAny<IFixedSizeList<byte>>(),
                    It.IsAny<Range>()))
                .Callback((IFixedSizeList<byte> groupBytes, Range linesRange) => 
                    Array.Copy(_lineIndexes, 0,
                               _storageLineIndexes, 
                               linesRange.Offset,
                               linesRange.Length));
            
            _groupStream = new MemoryStream(1024);
            _groupReader = new Mock<IReader>();
            _groupReader
                .Setup(o => o.Read(
                    It.IsAny<byte[]>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .Returns((byte[] buff, int offset, int count) =>
                        _groupStream.Read(buff, offset, count));

            _ioServiceMock = new Mock<IIoService>();
            _ioServiceMock
                .Setup(o => o.OpenRead(GroupFilePath))
                .Returns(_groupReader.Object);

            _groupInfoMock = new Mock<IGroupInfo>();
            _groupInfoMock
                .SetupGet(o => o.Name)
                .Returns(GroupFilePath);
            
            _configMock = new Mock<IConfig>();
            _configMock
                .SetupGet(o => o.IsLittleEndian)
                .Returns(BitConverter.IsLittleEndian);
            
            _configMock
                .SetupGet(o => o.GroupBufferRowReadingEnsurance)
                .Returns(7);
            
            _disposableValueMaker = new DisposableValueMaker();
            _buffersPool = new BuffersPool(
                _disposableValueMaker,
                _configMock.Object);

            _byteSegmentService = new ByteSegmentService();
            _sortingSegmentsSupplier = new SortingSegmentsSupplier<byte>(
                _linesStorageMock.Object,
                _byteSegmentService);

            _groupSorter = new GroupSorter<byte>(
                _sortingSegmentsSupplier, 
                _linesIndexesExtructor.Object,
                _linesStorageMock.Object,
                _byteSegmentService);
        }

        [Timeout(10000)]
        [TestCaseSource(nameof(Cases1))]
        public void Test(TestCase testCase)
        {
            _configMock
                .SetupGet(o => o.BufferSize)
                .Returns(testCase.BufferSize);

            _groupBytesLoader = new GroupBytesLoader(
                _buffersPool,
                _ioServiceMock.Object,
                _configMock.Object);

            _groupInfoMock
                .SetupGet(o => o.LinesCount)
                .Returns(testCase.InputLines.Length);
            
            _groupInfoMock
                .SetupGet(o => o.BytesCount)
                .Returns(testCase.GroupBytes.Length);

            _groupStream.Write(testCase.GroupBytes, 0,
                testCase.GroupBytes.Length);
            _groupStream.Position = 0;

            _lineIndexes = testCase.InputLines;

            var groupInfo = _groupBytesLoader
                .CalculateMatrixInfo(_groupInfoMock.Object);
            var group = _groupBytesLoader
                .LoadMatrix(groupInfo);

            _groupSorter.Sort(group, 
                new Range(LinesRangeOffset, testCase.InputLines.Length));

            var resultLines = new LineIndexes[testCase.InputLines.Length];
            Array.Copy(_storageLineIndexes, LinesRangeOffset,
                       resultLines, 0, testCase.InputLines.Length);
            
            Assert.AreEqual(
                testCase.ExpectedSortedLines, 
                resultLines.Select(o => o.start)
                );
        }

        public class TestCase
        {
            private readonly string _name;

            public TestCase(
                string name,
                InputLineList inputLines,
                int[] sortedLines,
                int bufferSize)
            {
                _name = name;

                InputLines = inputLines.CombineLinesIndexes();
                GroupBytes = inputLines.CombineLinesBytes();
                ExpectedSortedLines = sortedLines;

                // ExpectedSortedLines = Enumerable
                //     .Join(sortedLines, InputLines,
                //         o => o, o => o.start, (_, o) => o)
                //     .ToArray();

                BufferSize = bufferSize;
            }

            public LineIndexes[] InputLines { get; }
            public byte[] GroupBytes { get; }
            public int[] ExpectedSortedLines { get; }
            public int BufferSize { get; }

            public override string ToString() =>
                _name;

            public TestCase(
                string name,
                TestCase prototype,
                int bufferSize)
            {
                _name = name;
                InputLines = prototype.InputLines;
                GroupBytes = prototype.GroupBytes;
                ExpectedSortedLines = prototype.ExpectedSortedLines;
                BufferSize = bufferSize;
            }

            public class InputLineList
                : IEnumerable<InputLineList.Item>
            {
                private readonly IList<Item> _items =
                    new List<Item>();

                public void Add(
                    // start|letters count|digits count|sort by digits|offset
                    string indexes,
                    // xxnumber.string
                    string bytesView)
                {
                    var lineIndexes = LineIndexes.Parse(indexes);
                    var lineBytes = BytesOfString(bytesView);
                    lineBytes[0] = (byte) '\r';
                    lineBytes[1] = lineIndexes.digitsCount;

                    _items.Add(new Item(lineIndexes, lineBytes));
                }

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
    }
}
