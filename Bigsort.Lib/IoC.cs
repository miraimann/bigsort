using System;
using Bigsort.Contracts;
using Bigsort.Implementation;

namespace Bigsort.Lib
{
    public class IoC
    {
        internal IBigSorter BuildBigSorter<TSegment>(
            ISegmentService<TSegment> segmentService,
            IConfig config)

            where TSegment : IEquatable<TSegment>
                           , IComparable<TSegment>
        {
            IUsingHandleMaker usingHandleMaker =
                new UsingHandleMaker();

            ILinesReservation<TSegment> linesReservation =
                new LinesReservation<TSegment>(
                    usingHandleMaker, 
                    config);

            IGroupInfoMonoid groupInfoMonoid =
                new GroupInfoMonoid();

            IGroupsSummaryInfoMarger groupsSummaryInfoMarger = 
                new GroupsSummaryInfoMarger(
                    groupInfoMonoid);
            
            IPoolMaker poolMaker =
                new PoolMaker(
                    usingHandleMaker);

            IBuffersPool buffersPool =
                new BuffersPool(
                    poolMaker,
                    config);
            
            IoService ioService =
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
                    config);

            IGroupMatrixService groupMatrixService =
                new GroupMatrixService(
                    buffersPool,
                    config);

            ILinesStorage<TSegment> linesStorage =
                linesReservation;

            ISortingSegmentsSupplier sortingSegmentsSupplier =
                new SortingSegmentsSupplier<TSegment>(
                    linesStorage,
                    segmentService);

            ILinesIndexesExtractor linesIndexesExtractor =
                new LinesIndexesExtractor(
                    linesStorage);

            IGroupSorter groupSorter = 
                new GroupSorter<TSegment>(
                    sortingSegmentsSupplier,
                    linesIndexesExtractor,
                    linesStorage,
                    segmentService);

            ILinesIndexesStorage linesIndexesStorage =
                linesStorage;

            ISortedGroupWriter sortedGroupWriter =
                new SortedGroupWriter(
                    linesIndexesStorage);
            
            IMemoryOptimizer memoryOptimizer =
                new MemoryOptimizer(
                    groupMatrixService,
                    linesReservation,
                    buffersPool,
                    config);

            ISorter sorter1 = 
                new Sorter1(
                    linesReservation,
                    groupMatrixService,
                    groupSorter,
                    memoryOptimizer,
                    sortedGroupWriter,
                    ioService,
                    tasksQueue,
                    poolMaker,
                    config);

            ISorter sorter =
                new Sorter(
                    linesReservation,
                    groupMatrixService,
                    groupSorter,
                    sortedGroupWriter,
                    ioService,
                    tasksQueue,
                    poolMaker);

            IBigSorter bigSorter =
                new BigSorter(
                    ioService,
                    grouper,
                    sorter1);

            return bigSorter;
        }

        public IBigSorter BuildBigSorter()
        {
            IConfig config = new Config();

            if (config.SortingSegment == "byte")
                return BuildBigSorter(new ByteSegmentService(), config);

            if (config.SortingSegment == "uint")
                return BuildBigSorter(new UInt32SegmentService(), config);

            // if (config.SortingSegment == "ulong")
            return BuildBigSorter(new UInt64SegmentService(), config);
        }
    }
}
