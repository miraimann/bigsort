using System;
using Bigsort.Contracts;
using Bigsort.Implementation;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace Bigsort.Tests
{
    [TestFixture]
    public class IndexedInputServiceTests
    {
        private Mock<IConfig> _config;
        private IIndexedInputService _service;
        private IIoService _ioService;
        private Mock<IReadingStream> _readingStreamMock; 

        [SetUp]
        public void Setup()
        {
            _config = new Mock<IConfig>();
            _config.SetupGet(o => o.EndLine)
                   .Returns(Consts.EndLineBytes);

            _ioService = new IoService(_config.Object);
            _readingStreamMock = new Mock<IReadingStream>();

            _service = new IndexedInputService(_config.Object);
        }

        [TestCase(0,              // bytesCount
                  new long[] { }, // lineStarts
                  new long[] { }, // expectedLineEnds
            TestName = "0")] 

        [TestCase(654,                // bytesCount
                  new long[] {   0 }, // lineStarts
                  new long[] { 653 }, // expectedLineEnds
            TestName = "1")]

        [TestCase(654,                          // bytesCount
                  new long[] {  0,  123, 456 }, // lineStarts
                  new long[] { 122, 455, 653 }, // expectedLineEnds
            TestName = "2")]

        [TestCase(1111,                                       // bytesCount
                  new long[] {  0, 11,  56, 144, 276,  897 }, // lineStarts
                  new long[] { 10, 55, 143, 275, 896, 1110 }, // expectedLineEnds
            TestName = "3")]
        public void MakeInputTest(
            long bytesCount,
            long[] lineStarts,
            long[] expectedLineEnds)
        {
            _readingStreamMock
                .SetupGet(o => o.Length)
                .Returns(bytesCount);

            var result = _service
                .MakeInput(lineStarts, _readingStreamMock.Object);

            Assert.AreSame(_readingStreamMock.Object, result.Bytes);
            CollectionAssert.AreEqual(lineStarts, result.LinesStarts);
            CollectionAssert.AreEqual(expectedLineEnds, result.LinesEnds);
        }

        #region test cases

        [TestCase(new long[] { }, // coreLinesStarts
                  new long[] { }, // coreLinesEnds
                  new int[]  { }, // dotsShifts
                  new long[] { }, // expectedLinesStarts
                  true,           // withNewLineInTheFileEnd
            TestName = "0")] 

        [TestCase(new[] {  0L }, // coreLinesStarts
                  new[] { 17L }, // coreLinesEnds
                  new[] {   5 }, // dotsShifts
                  new[] {  6L }, // expectedLinesStarts
                  true,          // withNewLineInTheFileEnd
            TestName = "1")]

        [TestCase(new long[] {  0, 17 }, // coreLinesStarts
                  new long[] { 16, 32 }, // coreLinesEnds
                  new int[]  {  5,  6 }, // dotsShifts
                  new long[] {  6, 24 }, // expectedLinesStarts
                  true,                  // withNewLineInTheFileEnd
            TestName = "2")]

        [TestCase(new long[] {  0,  17, 123, 140, 231 }, // coreLinesStarts
                  new long[] { 16, 122, 139, 230, 236 }, // coreLinesEnds
                  new int[]  {  5,  39,   4,  81,   1 }, // dotsShifts
                  new long[] {  6,  57, 128, 222, 233 }, // expectedLinesStarts
                  true,                                  // withNewLineInTheFileEnd
            TestName = "3")]

        [TestCase(new long[] { }, // coreLinesStarts
                  new long[] { }, // coreLinesEnds
                  new int[]  { }, // dotsShifts
                  new long[] { }, // expectedLinesStarts
                  false,          // withNewLineInTheFileEnd
            TestName = "4")] 

        [TestCase(new[] {  0L }, // coreLinesStarts
                  new[] { 17L }, // coreLinesEnds
                  new[] {   5 }, // dotsShifts
                  new[] {  6L }, // expectedLinesStarts
                  false,         // withNewLineInTheFileEnd
            TestName = "5")]

        [TestCase(new long[] {  0, 17 }, // coreLinesStarts
                  new long[] { 16, 32 }, // coreLinesEnds
                  new int[]  {  5,  6 }, // dotsShifts
                  new long[] {  6, 24 }, // expectedLinesStarts
                  false,                 // withNewLineInTheFileEnd
            TestName = "6")]

        [TestCase(new long[] {  0,  17, 123, 140, 231 }, // coreLinesStarts
                  new long[] { 16, 122, 139, 230, 236 }, // coreLinesEnds
                  new int[]  {  5,  39,   4,  81,   1 }, // dotsShifts
                  new long[] {  6,  57, 128, 222, 233 }, // expectedLinesStarts
                  false,                                 // withNewLineInTheFileEnd
            TestName = "7")]

        // [TestCase(new long[] { 0 }, // coreLinesStarts
        //           new long[] { 1 }, // coreLinesEnds
        //           new int[]  { 1 }, // dotsShifts
        //           new long[] {   }, // expectedLinesStarts
        //           false,            // withNewLineInTheFileEnd
        //     TestName = "8")] 

        // [TestCase(new long[] { 0 }, // coreLinesStarts
        //           new long[] { 3 }, // coreLinesEnds
        //           new int[]  { 1 }, // dotsShifts
        //           new long[] {   }, // expectedLinesStarts
        //           false,            // withNewLineInTheFileEnd
        //     TestName = "9")] 
        
        [TestCase(new long[] { 0 }, // coreLinesStarts
                  new long[] { 3 }, // coreLinesEnds
                  new int[]  { 1 }, // dotsShifts
                  new long[] { 2 }, // expectedLinesStarts
                  true,             // withNewLineInTheFileEnd
            TestName = "10")] 
        
        [TestCase(new long[] { 0, 10 }, // coreLinesStarts
                  new long[] { 3, 13 }, // coreLinesEnds
                  new int[]  { 1,  1 }, // dotsShifts
                  new long[] { 2, 12 }, // expectedLinesStarts
                  true,                 // withNewLineInTheFileEnd
            TestName = "11")]
        
        // [TestCase(new long[] { 0, 10 }, // coreLinesStarts
        //           new long[] { 3, 13 }, // coreLinesEnds
        //           new int[]  { 1,  1 }, // dotsShifts
        //           new long[] {       }, // expectedLinesStarts
        //           false,                // withNewLineInTheFileEnd
        //     TestName = "12")]

        #endregion

        // TODO: move expected line ends to test cases 
        public void DecorateForStringsSortingTests(
            long[] coreLinesStarts,
            long[] coreLinesEnds,
            int[] dotsShifts,
            long[] expectedLinesStarts,
            bool withNewLineInTheFileEnd)
        {
            var end = Consts.EndLineBytes;
            var expectedLinesEnds = coreLinesEnds
                .Select(x => x - end.Length)
                .ToArray();

            var n = coreLinesStarts.Length;
            var bytes = _ioService.CreateInMemory();

            if (n != 0)
            {
                for (int i = 0; i <= coreLinesEnds[n - 1]; i++)
                    bytes.WriteByte((byte)')');

                if (withNewLineInTheFileEnd)
                {
                    bytes.Position -= end.Length;
                    foreach (var x in end)
                        bytes.WriteByte(x);
                }
                else expectedLinesEnds[n - 1] += end.Length;
            } 

            bytes.Position = 0;

            IIndexedInput core = new IndexedInput
            {
                LinesStarts = coreLinesStarts,
                LinesEnds = coreLinesEnds,
                Bytes = bytes
            };

            var result = _service
                .DecorateForStringsSorting(core, dotsShifts);

            Assert.AreSame(bytes, result.Bytes);
            CollectionAssert.AreEqual(expectedLinesStarts, result.LinesStarts);
            CollectionAssert.AreEqual(expectedLinesEnds, result.LinesEnds);
        }

        [TestCase(new long[] { }, // coreLinesStarts
                  new int[]  { }, // countsOfZerosInPrefix
                  new long[] { }, // coreLinesEnds
                  new int[]  { }, // dotsShifts
                  new long[] { }, // expectedLinesStarts
                  new long[] { }, // expectedLinesEnds
            TestName = "0")] 

        [TestCase(new long[] {  0 }, // coreLinesStarts
                  new int[]  {  0 }, // countsOfZerosInPrefix
                  new long[] { 32 }, // coreLinesEnds
                  new int[]  { 12 }, // dotsShifts
                  new long[] {  0 }, // expectedLinesStarts
                  new long[] { 11 }, // expectedLinesEnds
            TestName = "1")]

        [TestCase(new long[] {  0 }, // coreLinesStarts
                  new int[]  {  3 }, // countsOfZerosInPrefix
                  new long[] { 32 }, // coreLinesEnds
                  new int[]  { 17 }, // dotsShifts
                  new long[] {  3 }, // expectedLinesStarts
                  new long[] { 16 }, // expectedLinesEnds
            TestName = "2")]

                          //    0   1    2    3    4    5    6    7     8
        [TestCase(new long[] {  0, 32,  67, 123, 345, 456, 567, 789,  890 }, // coreLinesStarts
                  new int[]  {  3,  6,   0,   9,  31,  41, 222,   0,   78 }, // countsOfZerosInPrefix
                  new long[] { 31, 66, 122, 344, 455, 566, 788, 889, 1024 }, // coreLinesEnds
                  new int[]  { 17,  8,  13, 100,  62,  82, 222,   2,   80 }, // dotsShifts
                  new long[] {  3, 38,  67, 132, 376, 497, 788, 789,  968 }, // expectedLinesStarts
                  new long[] { 16, 39,  79, 222, 406, 537, 788, 790,  969 }, // expectedLinesEnds
            TestName = "3")]

        public void DecorateForNumbersSortingTests(
            long[] coreLinesStarts,
            int[] countsOfZerosInPrefix,
            long[] coreLinesEnds,
            int[] dotsShifts,
            long[] expectedLinesStarts,
            long[] expectedLinesEnds)
        {
            var bytes = _ioService.CreateInMemory();

            for (int i = 0; i < coreLinesStarts.Length; i++)
            {
                for (long j = coreLinesStarts[i];
                          j < coreLinesStarts[i] + countsOfZerosInPrefix[i];
                          j++, bytes.WriteByte((byte)'0'))
                    ;

                for (long j = coreLinesStarts[i] + countsOfZerosInPrefix[i];
                          j <= coreLinesEnds[i];
                          j++, bytes.WriteByte((byte)'1'))
                    ;
            }

            bytes.Position = 0;
            
            IIndexedInput core = new IndexedInput
            {
                LinesStarts = coreLinesStarts,
                LinesEnds = coreLinesEnds,
                Bytes = bytes
            };

            var result = _service
               .DecorateForNumbersSorting(core, dotsShifts);
            
            Assert.AreSame(bytes, result.Bytes);
            CollectionAssert.AreEqual(expectedLinesStarts, result.LinesStarts);
            CollectionAssert.AreEqual(expectedLinesEnds, result.LinesEnds);
        }
    }
}
