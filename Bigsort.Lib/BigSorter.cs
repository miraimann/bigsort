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

            IBuffersPool buffersPool =
                new BuffersPool(
                    config);

            IIoService ioService =
                new IoService(
                    buffersPool);

            ITasksQueue tasksQueue =
                new TasksQueue(
                    config);

            IInputReaderFactory inputReaderFactory =
                new InputReaderFactory(
                    ioService,
                    tasksQueue,
                    buffersPool,
                    config);

            IGroupsLinesOutputFactory groupsLinessOutputFactory =
                new GroupsLinesOutputFactory(
                    ioService,
                    tasksQueue,
                    buffersPool,
                    config);

            IGrouperIOs grouperIOs =
                new GrouperIOs(
                    inputReaderFactory,
                    groupsLinessOutputFactory,
                    ioService,
                    config);

            IGrouper grouper =
                new Grouper(
                    groupsInfoMarger,
                    grouperIOs,
                    tasksQueue,
                    config);

            IGroupsLoaderFactory groupsLoaderFactory =
                new GroupsLoaderFactory(
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
                    ioService,
                    config);

            ISorter sorter =
                new Sorter(
                    ioService,
                    grouper,
                    groupsLoaderFactory,
                    groupSorter,
                    sortedGroupWriterFactory,
                    config);

            sorter.Sort();
        }
    }
}
