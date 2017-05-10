using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;
using Bigsort.Implementation;
using Bigsort.Implementation.DevelopmentTools;

namespace Bigsort.Lib
{
    public static class IoC
    {
        internal static ISorter BuildSorter(
            string inputFilePath, 
            string outputFilePath)
        {
#if DEBUG
            ITimeTracker timeTracker = 
                new TimeTracker();

            IDiagnosticTools diagnosticTools = 
                new DiagnosticTools(
                    timeTracker);
#else
            IDiagnosticTools diagnosticTools = null;
#endif
            IConfig config = 
                new Config(
                    inputFilePath,
                    outputFilePath);
                  
            IGroupsInfoMarger groupsInfoMarger = 
                new GroupsInfoMarger(
                    diagnosticTools);
            
            IPoolMaker poolMaker =
                new PoolMaker();

            IBuffersPool buffersPool =
                new BuffersPool(
                    poolMaker,
                    config);
            
            IIoService ioService =
                new IoService(
                    buffersPool);

            ITasksQueue tasksQueue =
                new TasksQueue(
                    config);
            
            IInputReaderMaker inputReaderMaker =
                new InputReaderMaker(
                    ioService,
                    tasksQueue,
                    buffersPool,
                    config);

            IGroupsLinesWriterFactory groupsLinesWriterFactory =
                new GroupsLinesWriterFactory(
                    ioService,
                    tasksQueue,
                    poolMaker,
                    buffersPool,
                    config);

            IGrouperIOs grouperIOs =
                new GrouperIOs(
                    inputReaderMaker,
                    groupsLinesWriterFactory,
                    ioService,
                    config);

            IGrouper grouper =
                new Grouper(
                    groupsInfoMarger,
                    grouperIOs,
                    tasksQueue,
                    config,
                    diagnosticTools);
            
            IGroupsLoaderMaker groupsLoaderMaker =
                new GroupsLoaderMaker(
                    buffersPool,
                    ioService,
                    config,
                    diagnosticTools);
            
            ISortingSegmentsSupplier sortingSegmentsSupplier =
                new SortingSegmentsSupplier(
                    config,
                    diagnosticTools);

            ILinesIndexesExtractor linesIndexesExtractor =
                new LinesIndexesExtractor(
                    config,
                    diagnosticTools);

            IGroupSorter groupSorter = 
                new GroupSorter(
                    sortingSegmentsSupplier,
                    linesIndexesExtractor,
                    diagnosticTools);

            ISortedGroupWriterFactory sortedGroupWriterFactory =
                new SortedGroupWriterFactory(
                    poolMaker,
                    ioService,
                    config,
                    diagnosticTools);
            
            ISorter sorter =
                new Sorter(
                    ioService,
                    grouper, 
                    groupsLoaderMaker,
                    groupSorter,
                    sortedGroupWriterFactory,
                    config,
                    diagnosticTools);

            return sorter;
        }
    }
}
