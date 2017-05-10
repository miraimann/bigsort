using System.IO;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;
using Bigsort.Implementation;
using Bigsort.Implementation.DevelopmentTools;

namespace Bigsort.Lib
{
    public class IoC
    {
        internal ISorter BuildBigSorter(
            string inputFilePath, 
            string outputFilePath)
        {
            string groupsFilePath = Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName());
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
                    outputFilePath,
                    groupsFilePath);
                  
            IGroupsInfoMarger groupsSummaryInfoMarger = 
                new GroupsSummaryInfoMarger(
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
                    inputFilePath,
                    ioService,
                    tasksQueue,
                    buffersPool,
                    config);

            IGroupsLinesWriterFactory groupsLinesWriterFactory =
                new GroupsLinesWriterFactory(
                    groupsFilePath,
                    ioService,
                    tasksQueue,
                    poolMaker,
                    buffersPool,
                    config);

            IGrouperIOs grouperIOs =
                new GrouperIOs(
                    inputFilePath,
                    inputReaderMaker,
                    groupsLinesWriterFactory,
                    ioService,
                    config);

            IGrouper grouper =
                new Grouper(
                    groupsSummaryInfoMarger,
                    grouperIOs,
                    tasksQueue,
                    config,
                    diagnosticTools);
            
            IGroupsLoaderMaker groupsLoaderMaker =
                new GroupsLoaderMaker(
                    groupsFilePath,
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
                    outputFilePath,
                    poolMaker,
                    ioService,
                    config,
                    diagnosticTools);
            
            ISorter sorter =
                new Sorter(
                    inputFilePath,
                    outputFilePath,
                    groupsFilePath,
                    ioService,
                    grouper, 
                    groupsLoaderMaker,
                    groupSorter,
                    sortedGroupWriterFactory,
                    diagnosticTools);

            return sorter;
        }
    }
}
