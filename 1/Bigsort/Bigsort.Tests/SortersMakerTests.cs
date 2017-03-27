using Bigsort.Contracts;
using Bigsort.Implementation;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bigsort.Tests
{
    [TestFixture]
    public class SortersMakerTests
    {
        private ISortersMaker _sorterMaker;
        private IPoolMaker _poolMaker;
        private Mock<IConfig> _config;
        private IAccumulatorsFactory _accumulatorsFactory;
        private IBytesConvertersFactory _bytesConvertersFactory;
        private IIoService _ioService;

        [SetUp]
        public void Setup()
        {
            _config = new Mock<IConfig>();

            _config.SetupGet(o => o.EndLine)
                   .Returns(Consts.EndLineBytes);
            _config.SetupGet(o => o.MaxCollectionSize)
                   .Returns(16);
            _config.SetupGet(o => o.IntsAccumulatorFragmentSize)
                   .Returns(2);

            _poolMaker = new PoolMaker();
            _ioService = new IoService(_config.Object);
            _bytesConvertersFactory = new BytesConvertersFactory();

            _accumulatorsFactory = new AccumulatorsFactory(
                _poolMaker,
                _ioService,
                _bytesConvertersFactory,
                _config.Object);
            
            _sorterMaker = new SortersMaker(_accumulatorsFactory, _poolMaker);
        }

        [TestCase(new int[] { }, 
            TestName = "0")]

        [TestCase(new int[] { 1 },
            TestName = "1")]

        [TestCase(new int[] { 1, 2, 3, 4, 5 },
            TestName = "2")]

        [TestCase(new int[] { 14, 23, 35, 422, 5443 },
            TestName = "3")]
        public void NoSortSorterTests(int[] actualLines)
        {
            var orderdLines = new List<int>();
            var sorter = _sorterMaker.MakeNoSortSorter(
                orderdLines.Add);

            sorter.Sort(actualLines);

            CollectionAssert.AreEqual(actualLines, orderdLines);
        }

        [TestCase(
            new long[] { }, // linesStarts,
            new long[] { }, // linesEnds,
            new int[]  { }, // actualLines,
            new int[]  { }, // expectedOrderingResultView
            TestName = "0")]

        [TestCase(
            new long[] {  0 }, // linesStarts,
            new long[] { 17 }, // linesEnds,
            new int[]  {  0 }, // actualLines,
            new int[]  { +0 }, // expectedOrderingResultView
            TestName = "1")]
        
        [TestCase(
            new long[] {  0 }, // linesStarts,
            new long[] { 17 }, // linesEnds,
            new int[]  {    }, // actualLines,
            new int[]  { },    // expectedOrderingResultView
            TestName = "2")]

        [TestCase(
            new long[] {  0, 47 }, // linesStarts,
            new long[] { 17, 96 }, // linesEnds,
            new int[]  {  0     }, // actualLines,
            new int[]  { +0 },     // expectedOrderingResultView
            TestName = "3")]

        [TestCase(
            new long[] {  0, 47 }, // linesStarts,
            new long[] { 17, 96 }, // linesEnds,
            new int[]  {      1 }, // actualLines,
            new int[]  { +1 },     // expectedOrderingResultView
            TestName = "4")]

        [TestCase(
            new long[] {  0, 47 }, // linesStarts,
            new long[] { 17, 96 }, // linesEnds,
            new int[]  {  0,  1 }, // actualLines,
            new int[]  { +0, +1 }, // expectedOrderingResultView
            TestName = "5")]

        [TestCase(
            new long[] {  0, 47, 423 }, // linesStarts,
            new long[] { 17, 96, 425 }, // linesEnds,
            new int[]  {  0,  1,   2 }, // actualLines,
            new int[]  { +2, +0, +1 },  // expectedOrderingResultView
            TestName = "6")]

        [TestCase(
            new long[] {  0, 47, 423 }, // linesStarts,
            new long[] { 17, 96, 425 }, // linesEnds,
            new int[]  {  0,       2 }, // actualLines,
            new int[]  { +2, +0 },      // expectedOrderingResultView
            TestName = "7")]
        
        [TestCase(
            new long[] {  0, 47, 423 }, // linesStarts,
            new long[] { 17, 96, 425 }, // linesEnds,
            new int[]  {  0,  1      }, // actualLines,
            new int[]  { +0, +1 },      // expectedOrderingResultView
            TestName = "8")]
        
        [TestCase(
            new long[] { 10, 30,  90, 200 }, // linesStarts,
            new long[] { 27, 47, 107, 217 }, // linesEnds,
            new int[]  {  0,  1,   2,   3 }, // actualLines,
            new int[]  { -0, -1, -2, -3 },   // expectedOrderingResultView
            TestName = "9")]
        
        [TestCase(
            new long[] { 10, 30,  90, 200 }, // linesStarts,
            new long[] { 27, 47, 107, 217 }, // linesEnds,
            new int[]  {  0,       2,   3 }, // actualLines,
            new int[]  { -0, -2, -3 },       // expectedOrderingResultView
            TestName = "10")]
        
        [TestCase(
            new long[] { 10, 30,  90, 200 }, // linesStarts,
            new long[] { 27, 47, 107, 217 }, // linesEnds,
            new int[]  {      1,   2,   3 }, // actualLines,
            new int[]  { -1, -2, -3 },       // expectedOrderingResultView
            TestName = "11")]
        
        [TestCase(
            new long[] { 10, 30,  90, 200 }, // linesStarts,
            new long[] { 27, 47, 107, 217 }, // linesEnds,
            new int[]  {  0,  1,   2      }, // actualLines,
            new int[]  { -0, -1, -2 },       // expectedOrderingResultView
            TestName = "12")]

        [TestCase(
            new long[] { 10, 30,  90, 200, 300 }, // linesStarts,
            new long[] { 27, 47, 107, 217, 333 }, // linesEnds,
            new int[]  { 0,   1,   2,   3,   4 }, // actualLines,
            new int[]  { -0, -1, -2, -3, +4 },    // expectedOrderingResultView
            TestName = "13")]
        
        [TestCase(
            new long[] { 10, 30,  90, 200, 300 }, // linesStarts,
            new long[] { 27, 48, 107, 217, 333 }, // linesEnds,
            new int[]  {  0,  1,   2,   3,   4 }, // actualLines,
            new int[]  { -0, -2, -3, +1, +4 },    // expectedOrderingResultView
            TestName = "14")]
        
        [TestCase(
            new long[] { 10, 30,  90, 200, 300 }, // linesStarts,
            new long[] { 27, 45, 107, 217, 333 }, // linesEnds,
            new int[]  {  0,  1,   2,   3,   4 }, // actualLines,
            new int[]  { +1, -0, -2, -3, +4 },    // expectedOrderingResultView
            TestName = "15")]
        
        [TestCase(
            new long[] { 10, 30,  90, 200, 300 }, // linesStarts,
            new long[] { 27, 49, 107, 217, 333 }, // linesEnds,
            new int[]  { 0,        2,   3,   4 }, // actualLines,
            new int[]  { -0, -2, -3, +4 },        // expectedOrderingResultView
            TestName = "16")]
        
        [TestCase(
            new long[] { 10, 30,  90, 200, 300 }, // linesStarts,
            new long[] { 27, 45, 107, 217, 333 }, // linesEnds,
            new int[]  {      1,   2,   3,   4 }, // actualLines,
            new int[]  { +1, -2, -3, +4 },        // expectedOrderingResultView
            TestName = "17")]
        
        [TestCase(
            new long[] { 10, 30,  90, 200, 300 }, // linesStarts,
            new long[] { 27, 45, 107, 217, 333 }, // linesEnds,
            new int[]  {      1,   2,   3,   4 }, // actualLines,
            new int[]  { +1, -2, -3, +4 },        // expectedOrderingResultView
            TestName = "18")]
        
        [TestCase(
            new long[] { 1, 3, 5,  8, 12, 20, 30, 70 },    // linesStarts,
            new long[] { 2, 4, 7, 10, 15, 26, 40, 80 },    // linesEnds,
            new int[]  { 0, 1, 2,  3,  4,  5,  6,  7 },    // actualLines,
            new int[]  { -0, -1, -2, -3, +4, +5, -6, -7 }, // expectedOrderingResultView
            TestName = "19")]
        
        [TestCase(
            new long[] { 1, 4,  7,  9, 12, 20, 30, 70, 90 },   // linesStarts,
            new long[] { 3, 6, 10, 12, 19, 26, 40, 80, 91 },   // linesEnds,
            new int[]  { 0, 1,  2,  3,  4,  5,  6,  7,  8 },   // actualLines,
            new int[]  { +8, -0, -1, -2, -3, +5, +4, -6, -7 }, // expectedOrderingResultView
            TestName = "20")]
        
        [TestCase(
            new long[] { 10, 30,  90, 200, 300 }, // linesStarts,
            new long[] { 27, 47, 107, 217, 303 }, // linesEnds,
            new int[]  {  0,  1,   2,   3,   4 }, // actualLines,
            new int[]  { +4, -0,  -1, -2, -3 },   // expectedOrderingResultView
            TestName = "21")]

        [TestCase(
            new long[] { 1, 4,  7,  9, 12, 20, 30, 70, 90 }, // linesStarts,
            new long[] { 3, 6, 10, 12, 19, 26, 40, 80, 91 }, // linesEnds,
            new int[]  {    1,  2,  3,  4,  5,  6,  7,  8 }, // actualLines,
            new int[]  { +8, +1, -2, -3, +5, +4, -6, -7 },   // expectedOrderingResultView
            TestName = "22")]
        
        [TestCase(
            new long[] { 1, 4,  7,  9, 12, 20, 30, 70, 90 }, // linesStarts,
            new long[] { 3, 6, 10, 12, 19, 26, 40, 80, 91 }, // linesEnds,
            new int[]  { 0, 1,  2,  3,  4,  5,  6,  7     }, // actualLines,
            new int[]  { -0, -1, -2, -3, +5, +4, -6, -7 },   // expectedOrderingResultView
            TestName = "23")]
        
        
        [TestCase(
            new long[] { 1, 4,  7,  9, 12, 20, 30, 70, 90 }, // linesStarts,
            new long[] { 3, 6, 10, 12, 19, 26, 40, 80, 91 }, // linesEnds,
            new int[]  {    1,  2,  3,  4,  5,  6,  7     }, // actualLines,
            new int[]  { +1, -2, -3, +5, +4, -6, -7 },       // expectedOrderingResultView
            TestName = "24")]


        [TestCase(
            new long[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 }, // linesStarts,
            new long[] { 11, 24, 33, 47, 51, 66, 72, 83, 95 }, // linesEnds,
            new int[]  {  0,  1,  2,  3,  4,  5,  6,  7,  8 }, // actualLines,
            new int[]  { -0, -4, +6, -2, -7, +1, +8, +5, +3 }, // expectedOrderingResultView
            TestName = "25")]

        public void ByLengthSorterTests(
            long[] linesStarts,
            long[] linesEnds,
            int[] actualLines,
            int[] expectedOrderingResultView)
        // +x - x line was ordered, -x - x line was send to subsorting 
        {
            var orderingResultView = new List<int>();
            var input = new IndexedInput
            {
                LinesStarts = linesStarts,
                LinesEnds = linesEnds
            };

            var subSorter = new Mock<ISorter>();
            subSorter
                .Setup(o => o.Sort(It.IsAny<IEnumerable<int>>()))
                .Callback((IEnumerable<int> lines) =>
                                orderingResultView.AddRange(lines.Select(x => x * -1)));

            var sorter = _sorterMaker.MakeLengthSorter(
                input, orderingResultView.Add, subSorter.Object);
            sorter.Sort(actualLines);

            CollectionAssert.AreEqual(
                expectedOrderingResultView,
                orderingResultView);
        }

        #region test cases

        [TestCase(
            0,             // bytesCount
            new string[0], // strings
            new long[0],   // linesStarts,
            new int[0],    // actualLines,
            new int[0],    // expectedOrderingResultView
            TestName = "0")]
        
        [TestCase(
            100,           // bytesCount
            new[] { "a" }, // strings
            new[] { 10L }, // linesStarts,
            new[] {   0 }, // actualLines,
            new[] { +0 },  // expectedOrderingResultView
            TestName = "1")]


        [TestCase(
            100,           // bytesCount
            new[] {  "" }, // strings
            new[] { 10L }, // linesStarts,
            new[] {   0 }, // actualLines,
            new[] {  +0 }, // expectedOrderingResultView
            TestName = "2")]

        [TestCase(
            511,                               // bytesCount
            new[] { "a", "b", "c", "d", "e" }, // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +0, +1, +2, +3, +4 },      // expectedOrderingResultView
            TestName = "3")]
        
        [TestCase(
            511,                               // bytesCount
            new[] { "e", "b", "c", "d", "a" }, // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +4, +1, +2, +3, +0 },      // expectedOrderingResultView
            TestName = "4")]

        [TestCase(
            511,                               // bytesCount
            new[] { "e", "a", "b", "c", "d" }, // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +1, +2, +3, +4, +0 },      // expectedOrderingResultView
            TestName = "5")]

        [TestCase(
            511,                               // bytesCount
            new[] { "a", "c", "d", "b", "e" }, // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +0, +3, +1, +2, +4 },      // expectedOrderingResultView
            TestName = "6")]

        [TestCase(
            511,                               // bytesCount
            new[] { "c", "e", "d", "a", "b", }, // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +3, +4, +0, +2, +1 },      // expectedOrderingResultView
            TestName = "7")]

        [TestCase(
            511,                               // bytesCount
            new[] { "e", "d", "c", "b", "a" }, // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +4, +3, +2, +1, +0 },      // expectedOrderingResultView
            TestName = "8")]
                
        [TestCase(
            511,                               // bytesCount
            new[] 
            {
                "==========a",
                "==========b",
                "==========c",
                "==========d",
                "==========e"
            },                                 // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +0, +1, +2, +3, +4 },      // expectedOrderingResultView
            TestName = "9")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "==========e",
                "==========b",
                "==========c",
                "==========d",
                "==========a"
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +4, +1, +2, +3, +0 },      // expectedOrderingResultView
            TestName = "10")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "==========e",
                "==========a",
                "==========b",
                "==========c",
                "==========d"
            },                                 // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +1, +2, +3, +4, +0 },      // expectedOrderingResultView
            TestName = "11")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "==========a",
                "==========c",
                "==========d",
                "==========b",
                "==========e"
            },                                 // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +0, +3, +1, +2, +4 },      // expectedOrderingResultView
            TestName = "12")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "==========c",
                "==========e",
                "==========d",
                "==========a",
                "==========b"
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +3, +4, +0, +2, +1 },      // expectedOrderingResultView
            TestName = "13")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "==========e",
                "==========d",
                "==========c",
                "==========b",
                "==========a"
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +4, +3, +2, +1, +0 },      // expectedOrderingResultView
            TestName = "14")]
        
        [TestCase(
            511,                               // bytesCount
            new[] 
            {
                "a==========",
                "b==========",
                "c==========",
                "d==========",
                "e=========="
            },                                 // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +0, +1, +2, +3, +4 },      // expectedOrderingResultView
            TestName = "15")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "e==========",
                "b==========",
                "c==========",
                "d==========",
                "a=========="
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +4, +1, +2, +3, +0 },      // expectedOrderingResultView
            TestName = "16")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "e==========",
                "a==========",
                "b==========",
                "c==========",
                "d=========="
            },                                 // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +1, +2, +3, +4, +0 },      // expectedOrderingResultView
            TestName = "17")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "a==========",
                "c==========",
                "d==========",
                "b==========",
                "e=========="
            },                                 // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +0, +3, +1, +2, +4 },      // expectedOrderingResultView
            TestName = "18")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "c==========",
                "e==========",
                "d==========",
                "a==========",
                "b=========="
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +3, +4, +0, +2, +1 },      // expectedOrderingResultView
            TestName = "19")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "e==========",
                "d==========",
                "c==========",
                "b==========",
                "a=========="
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +4, +3, +2, +1, +0 },      // expectedOrderingResultView
            TestName = "20")]

        
        [TestCase(
            511,                               // bytesCount
            new[] 
            {
                "a==========",
                "b=========",
                "c========",
                "d======",
                "e====="
            },                                 // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +0, +1, +2, +3, +4 },      // expectedOrderingResultView
            TestName = "21")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "e========",
                "b======",
                "c==========",
                "d=========",
                "a=========="
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +4, +1, +2, +3, +0 },      // expectedOrderingResultView
            TestName = "22")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "e=========",
                "a========",
                "b======",
                "c==========",
                "d========"
            },                                 // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +1, +2, +3, +4, +0 },      // expectedOrderingResultView
            TestName = "23")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "a=========",
                "c=",
                "d=======",
                "b=====",
                "e===="
            },                                 // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +0, +3, +1, +2, +4 },      // expectedOrderingResultView
            TestName = "24")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "c=",
                "e==========",
                "d=",
                "a==========",
                "b="
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +3, +4, +0, +2, +1 },      // expectedOrderingResultView
            TestName = "25")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "e======",
                "d=========",
                "c=======",
                "b==========",
                "a======="
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +4, +3, +2, +1, +0 },      // expectedOrderingResultView
            TestName = "26")]

        [TestCase(
            511,                               // bytesCount
            new[] 
            {
                "==========a==========",
                "==========b==========",
                "==========c==========",
                "==========d==========",
                "==========e=========="
            },                                 // strings
            new[] { 10L,  40,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +0, +1, +2, +3, +4 },      // expectedOrderingResultView
            TestName = "27")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "==========e==========",
                "==========b==========",
                "==========c==========",
                "==========d==========",
                "==========a=========="
            },
            new[] { 10L,  40,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +4, +1, +2, +3, +0 },      // expectedOrderingResultView
            TestName = "28")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "==========e==========",
                "==========a==========",
                "==========b==========",
                "==========c==========",
                "==========d=========="
            },                                 // strings
            new[] { 10L,  40,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +1, +2, +3, +4, +0 },      // expectedOrderingResultView
            TestName = "29")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "==========a==========",
                "==========c==========",
                "==========d==========",
                "==========b==========",
                "==========e=========="
            },                                 // strings
            new[] { 10L,  40,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +0, +3, +1, +2, +4 },      // expectedOrderingResultView
            TestName = "30")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "==========c==========",
                "==========e==========",
                "==========d==========",
                "==========a==========",
                "==========b=========="
            },
            new[] { 10L,  40,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +3, +4, +0, +2, +1 },      // expectedOrderingResultView
            TestName = "31")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "==========e==========",
                "==========d==========",
                "==========c==========",
                "==========b==========",
                "==========a=========="
            },
            new[] { 10L,  40,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +4, +3, +2, +1, +0 },      // expectedOrderingResultView
            TestName = "32")]

        [TestCase(
            511,                               // bytesCount
            new[] 
            {
                "=",
                "==",
                "===",
                "====",
                "====="
            },                                 // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +0, +1, +2, +3, +4 },      // expectedOrderingResultView
            TestName = "33")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "=====",
                "==",
                "===",
                "====",
                "="
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +4, +1, +2, +3, +0 },      // expectedOrderingResultView
            TestName = "34")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "=====",
                "=",
                "==",
                "===",
                "===="
            },                                 // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +1, +2, +3, +4, +0 },      // expectedOrderingResultView
            TestName = "35")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "=",
                "===",
                "====",
                "==",
                "====="
            },                                 // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +0, +3, +1, +2, +4 },      // expectedOrderingResultView
            TestName = "36")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "===",
                "=====",
                "====",
                "=",
                "=="
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +3, +4, +0, +2, +1 },      // expectedOrderingResultView
            TestName = "37")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "=====",
                "====",
                "===",
                "==",
                "="
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +4, +3, +2, +1, +0 },      // expectedOrderingResultView
            TestName = "38")]

        [TestCase(
            511,                               // bytesCount
            new[] 
            {
                "",
                "==",
                "===",
                "====",
                "====="
            },                                 // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +0, +1, +2, +3, +4 },      // expectedOrderingResultView
            TestName = "39")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "=====",
                "==",
                "===",
                "====",
                ""
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +4, +1, +2, +3, +0 },      // expectedOrderingResultView
            TestName = "40")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "=====",
                "=",
                "==",
                "===",
                "===="
            },                                 // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +1, +2, +3, +4, +0 },      // expectedOrderingResultView
            TestName = "41")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "",
                "===",
                "====",
                "==",
                "====="
            },                                 // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +0, +3, +1, +2, +4 },      // expectedOrderingResultView
            TestName = "42")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "===",
                "=====",
                "====",
                "",
                "=="
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +3, +4, +0, +2, +1 },      // expectedOrderingResultView
            TestName = "43")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "=====",
                "====",
                "===",
                "==",
                ""
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { +4, +3, +2, +1, +0 },      // expectedOrderingResultView
            TestName = "44")]

        [TestCase(
            511,                             // bytesCount
            new[] {  "", "", "",  "",  "" }, // strings
            new[] { 10L, 30, 90, 200, 300 }, // linesStarts,
            new[] {   0,  1,  2,   3,   4 }, // actualLines,
            new[] { -0, -1, -2, -3, -4 },    // expectedOrderingResultView
            TestName = "45")]
        
        [TestCase( 
            511,                                // bytesCount
            new[] { "a", "a", "a",  "a", "a" }, // strings
            new[] { 10L,  30,  90,  200, 300 }, // linesStarts,
            new[] {   0,   1,   2,    3,   4 }, // actualLines,
            new[] { -0, -1, -2, -3, -4 },       // expectedOrderingResultView
            TestName = "46")]

        [TestCase(
            511,                              // bytesCount
            new[]
            {
                "=====",
                "=====",
                "=====",
                "=====",
                "====="
            },
            new[] { 10L, 30, 90, 200, 300 }, // linesStarts,
            new[] {   0,  1,  2,   3,   4 }, // actualLines,
            new[] {  -0, -1, -2,  -3,  -4 }, // expectedOrderingResultView
            TestName = "47")]

        [TestCase(
            511,                               // bytesCount
            new[] { "a", "b", "c", "d", "e" }, // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,             3,   4 }, // actualLines,
            new[] { +0, +3, +4 },              // expectedOrderingResultView
            TestName = "48")]
        
        [TestCase(
            511,                               // bytesCount
            new[] { "e", "b", "c", "d", "a" }, // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {        1,   2,   3,   4 }, // actualLines,
            new[] { +4, +1, +2, +3 },          // expectedOrderingResultView
            TestName = "49")]

        [TestCase(
            511,                               // bytesCount
            new[] { "e", "a", "b", "c", "d" }, // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3      }, // actualLines,
            new[] { +1, +2, +3, +0 },          // expectedOrderingResultView
            TestName = "50")]

        [TestCase(
            511,                               // bytesCount
            new[] { "a", "c", "d", "b", "e" }, // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {             2,   3      }, // actualLines,
            new[] { +3, +2 },                  // expectedOrderingResultView
            TestName = "51")]

        [TestCase(
            511,                               // bytesCount
            new[] { "c", "e", "d", "a", "b" }, // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {                       4 }, // actualLines,
            new[] { +4 },                      // expectedOrderingResultView
            TestName = "52")]

        [TestCase(
            511,                               // bytesCount
            new[] { "e", "d", "c", "b", "a" }, // strings
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,        2,        4 }, // actualLines,
            new[] { +4, +2, +0 },              // expectedOrderingResultView
            TestName = "53")]

        [TestCase(
            511,                             // bytesCount
            new[] {  "", "", "",  "",  "" }, // strings
            new[] { 10L, 30, 90, 200, 300 }, // linesStarts,
            new[] {   0,           3,   4 }, // actualLines,
            new[] { -0, -3, -4 },    // expectedOrderingResultView
            TestName = "54")]
        
        [TestCase( 
            511,                                // bytesCount
            new[] { "a", "a", "a",  "a", "a" }, // strings
            new[] { 10L,  30,  90,  200, 300 }, // linesStarts,
            new[] {   0,   1,   2,    3,     }, // actualLines,
            new[] { -0, -1, -2, -3 },           // expectedOrderingResultView
            TestName = "55")]

        [TestCase(
            511,                              // bytesCount
            new[]
            {
                "=====",
                "=====",
                "=====",
                "=====",
                "====="
            },
            new[] { 10L, 30, 90, 200, 300 }, // linesStarts,
            new[] {       1,  2,   3,   4 }, // actualLines,
            new[] { -1, -2,  -3,  -4 },      // expectedOrderingResultView
            TestName = "56")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "==========a",
                "==========a",
                "==========b",
                "==========b",
                "==========b"
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { -0, -1, -2, -3, -4 },      // expectedOrderingResultView
            TestName = "57")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "==========b==",
                "==========b==",
                "==========b==",
                "==========a=======",
                "==========a======="
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { -3, -4, -0, -1, -2 },      // expectedOrderingResultView
            TestName = "58")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "==========b=======",
                "==========a==",
                "==========b=======",
                "==========a==",
                "==========b======="
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { -1, -3, -0, -2, -4 },      // expectedOrderingResultView
            TestName = "59")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "==========b=======",
                "==========a==",
                "==========c=======",
                "==========a==",
                "==========b======="
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { -1, -3, -0, -4, +2 },      // expectedOrderingResultView
            TestName = "60")]

        [TestCase(
            511,                             // bytesCount
            new[]
            {
                "==========b=======",
                "==========a==",
                "==================",
                "==========a==",
                "==========b======="
            },
            new[] { 10L, 30, 90, 200, 300 }, // linesStarts,
            new[] {   0,  1,  2,   3,   4 }, // actualLines,
            new[] { +2, -1, -3, -0, -4 },    // expectedOrderingResultView
            TestName = "61")]

        [TestCase(
            511,                             // bytesCount
            new[]
            {
                "==========b=======",
                "==========a==",
                "==========c=======",
                "==========a==",
                "==========d======="
            },
            new[] { 10L, 30, 90, 200, 300 }, // linesStarts,
            new[] {   0,  1,  2,   3,   4 }, // actualLines,
            new[] { -1, -3, +0, +2, +4 },    // expectedOrderingResultView
            TestName = "62")]

        [TestCase(
            511,                             // bytesCount
            new[]
            {
                "==========c=======",
                "==========a==",
                "==========d=======",
                "==========a==",
                "==========b======="
            },
            new[] { 10L, 30, 90, 200, 300 }, // linesStarts,
            new[] {   0,  1,  2,   3,   4 }, // actualLines,
            new[] { -1, -3, +4, +0, +2 },    // expectedOrderingResultView
            TestName = "63")]

        [TestCase(
            511,                                  // bytesCount
            new[]
            {
                "==========d=======",
                "==========a==",
                "==========b=======",
                "==========a==",
                "==========c=======",
                "==========c======="
            },
            new[] { 10L, 30, 90, 200, 300, 400 }, // linesStarts,
            new[] {   0,  1,  2,   3,   4,   5 }, // actualLines,
            new[] { -1, -3, +2, -4, -5, +0 },     // expectedOrderingResultView
            TestName = "64")]

        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "=b=",
                "=b=",
                "=b=",
                "=a==",
                "=a=="
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { -3, -4, -0, -1, -2 },      // expectedOrderingResultView
            TestName = "65")]
        
        [TestCase(
            511,                               // bytesCount
            new[]
            {
                "=",
                "=",
                "=",
                "",
                ""
            },
            new[] { 10L,  30,  90, 200, 300 }, // linesStarts,
            new[] {   0,   1,   2,   3,   4 }, // actualLines,
            new[] { -3, -4, -0, -1, -2 },      // expectedOrderingResultView
            TestName = "66")]

        #endregion

        public void SymbolBySymbolSorterTest(
            long bytesCount,
            string[] strings,
            long[] linesStarts,
            int[] actualLines,
            int[] expectedOrderingResultView)
        // +x - x line was ordered, -x - x line was send to subsorting 
        {
            var someSimbol = (byte) '-';
            var orderingResultView = new List<int>();

            var linesEnds = Enumerable
                .Zip(linesStarts, 
                     strings.Select(Enumerable.Count),
                     (a, b) => a + b - 1)
                .ToList();

            var inputStream = new MemoryStream();

            int j = 0;
            for (int i = 0; i < strings.Length; i++)
            {
                for (; j < linesStarts[i]; j++)
                    inputStream.WriteByte(someSimbol);

                for (int k = 0; k < strings[i].Length; j++, k++)
                    inputStream.WriteByte((byte)strings[i][k]);
            }

            for (; j < bytesCount; j++)
                inputStream.WriteByte(someSimbol);

            inputStream.Position = 0;

            var bytes = _ioService.Adapt(inputStream);
            var input = new IndexedInput
            {
                LinesStarts = linesStarts,
                LinesEnds = linesEnds,
                Bytes = bytes
            };

            var subSorter = new Mock<ISorter>();
            subSorter
                .Setup(o => o.Sort(It.IsAny<IEnumerable<int>>()))
                .Callback((IEnumerable<int> lines) =>
                                orderingResultView.AddRange(lines.Select(x => x * -1)));

            var sorter = _sorterMaker.MakeSymbolBySymbolSorter(
                input, orderingResultView.Add, subSorter.Object);
            sorter.Sort(actualLines);

            CollectionAssert.AreEqual(
                expectedOrderingResultView,
                orderingResultView);
        }
    }
}
