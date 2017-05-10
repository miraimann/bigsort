using Bigsort.Contracts;
using Bigsort.Implementation;

namespace Bigsort.Lib
{
    public class BigSorter
    {
        public static void Sort(string input, string output)
        {
            IConfig config =
                new Config(
                    input,
                    output);

            IGroupsInfoMarger groupsInfoMarger =
                new GroupsInfoMarger();

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
                    config);

            IGroupsLoaderMaker groupsLoaderMaker =
                new GroupsLoaderMaker(
                    buffersPool,
                    ioService,
                    config);

            ISortingSegmentsSupplier sortingSegmentsSupplier =
                new SortingSegmentsSupplier(
                    config);

            ILinesIndexesExtractor linesIndexesExtractor =
                new LinesIndexesExtractor(
                    config);

            IGroupSorter groupSorter =
                new GroupSorter(
                    sortingSegmentsSupplier,
                    linesIndexesExtractor);

            ISortedGroupWriterFactory sortedGroupWriterFactory =
                new SortedGroupWriterFactory(
                    poolMaker,
                    ioService,
                    config);

            ISorter sorter =
                new Sorter(
                    ioService,
                    grouper,
                    groupsLoaderMaker,
                    groupSorter,
                    sortedGroupWriterFactory,
                    config);

            sorter.Sort();
        }
    }
}
