using System;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;
using Bigsort.Implementation;
using Bigsort.Implementation.DevelopmentTools;

namespace Bigsort.Lib
{
    public class IoC
    {
        internal IBigSorter BuildBigSorter<TSegment>(
            ISegmentService<TSegment> segmentService)

            where TSegment : IEquatable<TSegment>
                           , IComparable<TSegment>
        {
            IConfig config = 
                new Config();

            ITimeTracker timeTracker =
                new TimeTracker();

            IDiagnosticTools diagnosticTools =
                new DiagnosticTools(
                    timeTracker);

            IUsingHandleMaker usingHandleMaker =
                new UsingHandleMaker();

            ILinesReservation<TSegment> linesReservation =
                new LinesReservation<TSegment>(
                    usingHandleMaker, 
                    config,
                    diagnosticTools);
            
            IGroupsSummaryInfoMarger groupsSummaryInfoMarger = 
                new GroupsSummaryInfoMarger(
                    diagnosticTools);
            
            IPoolMaker poolMaker =
                new PoolMaker(
                    usingHandleMaker);

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
            
            IGrouperBuffersProviderMaker grouperBuffersProviderMaker =
                new GrouperBuffersProviderMaker(
                    buffersPool,
                    ioService,
                    usingHandleMaker,
                    tasksQueue,
                    config);

            IGroupsLinesWriterMaker groupsLinesWriterMaker =
                new GroupsLinesWriterMaker(
                    ioService,
                    buffersPool,
                    tasksQueue,
                    config);

            IGrouperIOMaker grouperIoMaker =
                new GrouperIOMaker(
                    grouperBuffersProviderMaker,
                    groupsLinesWriterMaker,
                    ioService,
                    config);

            IGrouper grouper =
                new Grouper(
                    groupsSummaryInfoMarger,
                    grouperIoMaker,
                    tasksQueue,
                    config,
                    diagnosticTools);

            IMemoryOptimizer memoryOptimizer =
                new MemoryOptimizer(
                    linesReservation,
                    buffersPool,
                    config);

            IGroupsService groupsService =
                new GroupsService(
                    buffersPool,
                    linesReservation,
                    poolMaker,
                    ioService,
                    tasksQueue,
                    memoryOptimizer,
                    config,
                    diagnosticTools);

            ILinesStorage<TSegment> linesStorage =
                linesReservation;

            ISortingSegmentsSupplier sortingSegmentsSupplier =
                new SortingSegmentsSupplier<TSegment>(
                    linesStorage,
                    segmentService,
                    diagnosticTools);

            ILinesIndexesExtractor linesIndexesExtractor =
                new LinesIndexesExtractor(
                    linesStorage,
                    diagnosticTools);

            IGroupSorter groupSorter = 
                new GroupSorter<TSegment>(
                    sortingSegmentsSupplier,
                    linesIndexesExtractor,
                    linesStorage,
                    segmentService,
                    diagnosticTools);

            ILinesIndexesStorage linesIndexesStorage =
                linesStorage;

            ISortedGroupWriterMaker sortedGroupWriterMaker =
                new SortedGroupWriterMaker(
                    ioService,
                    poolMaker,
                    linesIndexesStorage,
                    diagnosticTools);

            ISorter sorter1 = 
                new Sorter(
                    groupsService,
                    groupSorter,
                    sortedGroupWriterMaker,
                    tasksQueue);

            ISorter sorter =
                new Sorter(
                    linesReservation,
                    groupsService,
                    groupSorter,
                    sortedGroupWriterMaker,
                    ioService,
                    tasksQueue,
                    poolMaker);

            IBigSorter bigSorter =
                new BigSorter(
                    ioService,
                    grouper,
                    sorter,
                    diagnosticTools);

            return bigSorter;
        }

        public IBigSorter BuildBigSorter()
        {
            IConfig config = new Config();

            if (config.SortingSegment == "byte")
                return BuildBigSorter(new ByteSegmentService());

            if (config.SortingSegment == "uint")
                return BuildBigSorter(new UInt32SegmentService());

            // if (config.SortingSegment == "ulong")
            return BuildBigSorter(new UInt64SegmentService());
        }
    }
}
