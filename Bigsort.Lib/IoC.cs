using System;
using Bigsort.Contracts;
using Bigsort.Implementation;

namespace Bigsort.Lib
{
    public class IoC
    {
        internal ISorter BuildSorter<TSegment>(
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

            IGroupBytesMatrixService groupBytesMatrixService =
                new GroupBytesMatrixService(
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

            ISorter sorter =
                new Sorter<TSegment>(
                    linesReservation,
                    grouper,
                    groupBytesMatrixService,
                    groupSorter,
                    sortedGroupWriter,
                    ioService,
                    tasksQueue,
                    poolMaker,
                    config);

            return sorter;
        }

        public ISorter BuildSorter()
        {
            IConfig config = new Config();

            if (config.SortingSegment == "byte")
                return BuildSorter(new ByteSegmentService(), config);

            if (config.SortingSegment == "uint")
                return BuildSorter(new UInt32SegmentService(), config);

            // if (config.SortingSegment == "ulong")
            return BuildSorter(new UInt64SegmentService(), config);
        }
    }
}
