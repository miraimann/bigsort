using System;
using Bigsort.Contracts;
using System.Linq;

namespace Bigsort.Implementation
{
    internal class LinesSorter
        : ILinesSorter
    {
        private readonly IIoService _ioService;
        private readonly ILinesIndexator _linesIndexator;
        private readonly IIndexedInputService _indexedInputService;
        private readonly IAccumulatorsFactory _accumulatorsFactory;
        private readonly ISortersMaker _sortersMaker;
        private readonly IResultWriter _resultWriter;
        private readonly ITextWriter _logger;
        public LinesSorter(
            IIoService ioService,
            ILinesIndexator linesIndexator,
            ISortersMaker sortersMaker, 
            IIndexedInputService indexedInputService,
            IAccumulatorsFactory accumulatorsFactory,
            IResultWriter resultWriter,
            ITextWriter logger = null)
        {
            _ioService = ioService;
            _linesIndexator = linesIndexator;
            _sortersMaker = sortersMaker;
            _indexedInputService = indexedInputService;
            _accumulatorsFactory = accumulatorsFactory;
            _resultWriter = resultWriter;
            _logger = logger;
        }

        public void Sort(string inputFile, string outputFile)
        {
            const byte zeroDigitByte = (byte)'0',
                       spaceDigitByte = (byte)' ';

            var globalStart = DateTime.Now;
            _logger?.WriteLine("started");

            using (var linesStarts = _accumulatorsFactory.CreateCacheableForLong())
            using (var dotsShifts = _accumulatorsFactory.CreateCacheableForInt())
            {
                var start = DateTime.Now;
                _logger?.WriteLine("input indexing was started");

                _linesIndexator.IndexLines(
                    _ioService.EnumeratesBytesOf(inputFile),
                    outLinesStart: linesStarts.Add,
                    outDotShift: dotsShifts.Add);

                _logger?.WriteLine(
                    "input indexing was finished: {0}",
                    DateTime.Now - start);

                using (var linesOrdering = _accumulatorsFactory.CreateCacheableForInt())
                using (var inputBytes = _ioService.OpenRead(inputFile))
                {
                    IIndexedInput
                        input = _indexedInputService
                            .MakeInput(linesStarts, inputBytes),

                        inputForNumbersSorting = _indexedInputService
                            .DecorateForNumbersSorting(input, dotsShifts),

                        inputForStringsSorting = _indexedInputService
                            .DecorateForStringsSorting(input, dotsShifts);

                    ISorter
                        noSorter = _sortersMaker
                            .MakeNoSortSorter(
                                nextLineFound: linesOrdering.Add),

                        numbersDigitByDigitSorter = _sortersMaker
                            .MakeSymbolBySymbolSorter(
                                inputForNumbersSorting,
                                nextLineFound: linesOrdering.Add,
                                subSorter: noSorter,
                                hashFunc: x => x - zeroDigitByte,
                                abcLength: 10 /* '9' - '0' + 1 */),

                        numberByLengthSorter = _sortersMaker
                            .MakeLengthSorter(
                                inputForNumbersSorting,
                                nextLineFound: linesOrdering.Add,
                                subSorter: numbersDigitByDigitSorter),

                        stringsSorter = _sortersMaker
                            .MakeSymbolBySymbolSorter(
                                inputForStringsSorting,
                                nextLineFound: linesOrdering.Add,
                                subSorter: numberByLengthSorter,
                                hashFunc: x => x - spaceDigitByte,
                                abcLength: 96 /* '~' - ' ' + 1 */);

                    start = DateTime.Now;
                    _logger?.WriteLine("sorting was started");

                    stringsSorter.Sort(Enumerable.Range(0, linesStarts.Count));
                    
                    _logger?.WriteLine(
                        "sorting was finished: {0}",
                        DateTime.Now - start);

                    start = DateTime.Now;
                    _logger?.WriteLine("result writing was started");

                    using (var output = _ioService.OpenWrite(outputFile))
                        _resultWriter.Write(input, output, linesOrdering);

                    _logger?.WriteLine(
                        "result writing was finished: {0}",
                        DateTime.Now - start);
                    
                    _logger?.WriteLine(
                        "finished: {0}",
                        DateTime.Now - globalStart);
                }
            }
        }
    }
}
