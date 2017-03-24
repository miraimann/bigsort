using System.IO;
using Bigsort.Contracts;
using Bigsort.Implementation;

namespace Bigsort
{
    internal class IoC
    {
        public IoC(TextWriter logger = null)
        {
            var config = new Config();
            var ioService = new IoService(config);
            
            var appLogger =
                logger == null
                    ? null
                    : ioService.Adapt(logger);

            var poolMaker = new PoolMaker();
            var bytesConvertersFactory = new BytesConvertersFactory();
            var linesIndexator = new LinesIndexator(config);
            var accumelatorsFactory = 
                new AccumulatorsFactory(
                    poolMaker,
                    ioService,
                    bytesConvertersFactory,
                    config);
            var sortersMaker = new SortersMaker(accumelatorsFactory, poolMaker);
            var indexedInputService = new IndexedInputService(config);
            var resultWriter = new ResultWriter(config);
            
            LinesSorter = 
                new LinesSorter(
                    ioService,
                    linesIndexator,
                    sortersMaker,
                    indexedInputService,
                    accumelatorsFactory,
                    resultWriter,
                    appLogger);
        }

        public ILinesSorter LinesSorter { get; }
    }
}
