using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bigsort.Contracts;
using Bigsort.Implementation;
using Bigsort.Tools.SortedFileChecker;
using Castle.Core.Internal;
using Moq;
using NUnit.Framework;
using static Bigsort.Tests.Tools;
using static NUnit.Framework.TestContext;
using Range = Bigsort.Contracts.Range;

namespace Bigsort.Tests
{
    public partial class GroupSorterTests
    {
        public class Integration
        {
            private const string
                GroupId = "xo",
                WorkingDirectory = "E:\\GroupSorterTests",
                ResultFileSufix = "_sorted";

            private const int ForByte = 0, ForUInt32 = 1, ForUInt64 = 2;
            private static readonly string[] Names =
            {
                "byte", "uint", "ulong"
            };

            private const int
                BufferSize = 32 * 1024,
                MaxMemoryForLines = 512 * 1024 * 1024;

            private Setup<byte> _sorterByByteSetup;
            private Setup<uint> _sorterByUInt32Setup;
            private Setup<ulong> _sorterByUInt64Setup;

            private class Setup<T>
                where T : IEquatable<T>
                , IComparable<T>
            {
                public Mock<IConfig> ConfigMock;

                public IBuffersPool BuffersPool;
                public IUsingHandleMaker UsingHandleMaker;
                public ILinesReservation<T> LinesReservation;
                public ILinesIndexesExtractor LinesIndexesExtractor;
                public ISegmentService<T> SegmentService;
                public ISortingSegmentsSupplier SegmentsSupplier;
                public IGroupMatrixService GroupMatrixService;
                public IIoService IoService;
                public ISortedGroupWriter SortedGroupWriter;
                public IGroupSorter Sorter;

                public Setup(ISegmentService<T> segmentService)
                {
                    SegmentService = segmentService;

                    ConfigMock = new Mock<IConfig>();
                    ConfigMock
                        .SetupGet(o => o.GroupBufferRowReadingEnsurance)
                        .Returns(GroupBufferRowReadingEnsurance);

                    ConfigMock
                        .SetupGet(o => o.BufferSize)
                        .Returns(BufferSize);

                    ConfigMock
                        .SetupGet(o => o.MaxMemoryForLines)
                        .Returns(MaxMemoryForLines);

                    UsingHandleMaker = new UsingHandleMaker();
                    LinesReservation = new LinesReservation<T>(
                        UsingHandleMaker,
                        ConfigMock.Object);

                    LinesIndexesExtractor = new LinesIndexesExtractor(
                        LinesReservation);

                    SegmentsSupplier = new SortingSegmentsSupplier<T>(
                        LinesReservation,
                        SegmentService);

                    Sorter = new GroupSorter<T>(
                        SegmentsSupplier,
                        LinesIndexesExtractor,
                        LinesReservation,
                        SegmentService);

                    BuffersPool = new InfinityBuffersPool(BufferSize);
                    IoService = new IoService(BuffersPool);
                    GroupMatrixService = new GroupMatrixService(
                        BuffersPool,
                        ConfigMock.Object);

                    SortedGroupWriter = new SortedGroupWriter(
                        LinesReservation);
                }
            }

            [SetUp]
            public void SetUp()
            {
                _sorterByByteSetup = new Setup<byte>(
                    new ByteSegmentService());

                _sorterByUInt32Setup = new Setup<uint>(
                    new UInt32SegmentService());

                _sorterByUInt64Setup = new Setup<ulong>(
                    new UInt64SegmentService());
            }

            [TestCase(32, 128, 100,
                new [] { ForByte, ForUInt32, ForUInt64 }, true
                , Ignore = "for hands run only"
            )]

            [TestCase(128, 225, 10000,
                new[] { ForUInt64 }, true
                , Ignore = "for hands run only"
             )]

            [TestCase(128, 225, 100000,
                new[] { ForUInt64 }, true
                , Ignore = "for hands run only"
             )]

            [TestCase(128, 225, 1000000,
                new[] { ForUInt64 }, true
                , Ignore = "for hands run only"
             )]

            [TestCase(128, 225, 10000000, 
                new [] { ForUInt64 }, true
                , Ignore = "for hands run only"
             )]

            public void Test(
                int maxNumberLength,
                int maxStringLength,
                int linesCount,
                int[] actualSorters,
                bool clear = true)
            {
                var runners = new[]
                    {
                        RunnerFor(_sorterByByteSetup),
                        RunnerFor(_sorterByUInt32Setup),
                        RunnerFor(_sorterByUInt64Setup)
                    }
                    .Where((_, i) => Array.IndexOf(actualSorters, i) >= 0)
                    .ToArray();

                var names = actualSorters
                    .Select(i => Names[i])
                    .ToArray();

                var subWorkingDirectories = names
                    .Select(name => Path.Combine(WorkingDirectory, name))
                    .ToArray();

                var inputPathes = subWorkingDirectories
                    .Select(dir => Path.Combine(dir, GroupId))
                    .ToArray();

                var outputPathes = inputPathes
                    .Select(origin => origin + ResultFileSufix)
                    .ToArray();

                if (!Directory.Exists(WorkingDirectory))
                    Directory.CreateDirectory(WorkingDirectory);

                var prevDirectory = Environment.CurrentDirectory;
                Environment.CurrentDirectory = WorkingDirectory;
                foreach (var dir in subWorkingDirectories)
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                try
                {
                    foreach (var file in inputPathes)
                        if (File.Exists(file))
                            File.Delete(file);

                    var lines = GroupLinesGenerator
                        .Generate("xo", linesCount, maxNumberLength, maxStringLength)
                        .ToArray();
                    
                    using (var stream = File.OpenWrite(inputPathes[0]))
                        foreach (var line in lines)
                            stream.Write(line, 0, line.Length);
                    
                    foreach (var i in actualSorters.Skip(1))
                        File.Copy(inputPathes[0], inputPathes[i]);

                    var bytesCount = lines.Sum(line => line.Length);
                    var groupsInfoMock = new Mock<IGroupInfo>();
                    groupsInfoMock
                        .Setup(o => o.LinesCount)
                        .Returns(linesCount);
                    groupsInfoMock
                        .Setup(o => o.BytesCount)
                        .Returns(bytesCount);
                    groupsInfoMock
                        .Setup(o => o.Mapping)
                        .Returns(new[] {new LongRange(0, bytesCount)});
                    
                    Out?.WriteLine($"input size: {bytesCount}");
                    Out?.WriteLine();

                    var testResult =
                        names.Zip(inputPathes, (logPrefix, input) => new { logPrefix, input })
                             .Zip(outputPathes, (o, output) => new { o.logPrefix, o.input, output })
                             .Zip(runners, (o, runner) => runner(groupsInfoMock.Object, o.input, o.output, o.logPrefix))
                             .Zip(names, (success, name) => new { success, name })
                             .ToArray();

                    Assert.IsTrue(
                        testResult.All(o => o.success),
                        testResult.Where(o => !o.success)
                                  .Select(o => o.name)
                                  .Aggregate("failed = {", (acc, o) => $"{acc} {o}") + " }"
                                  );
                }
                finally
                {
                    Environment.CurrentDirectory = prevDirectory;
                    if (clear && Directory.Exists(WorkingDirectory))
                        Directory.Delete(WorkingDirectory, true);
                }
            }

            private Func<IGroupInfo, string, string, string, bool> RunnerFor<T>(Setup<T> setup)
                where T : IEquatable<T>, IComparable<T> => (group, inputPath, outputPath, logPrefix) =>
            {
                var t = DateTime.Now;
                IGroupMatrix matrix;
                if (!setup.GroupMatrixService.TryCreateMatrix(group, out matrix))
                    return false;

                using (var reader = setup.IoService.OpenRead(inputPath))
                    setup.GroupMatrixService.LoadGroupToMatrix(matrix, group, reader);

                Out?.WriteLine($"[{logPrefix}] group loading time: " +
                               $"{DateTime.Now - t}");

                setup.LinesReservation.Load(group.BytesCount);
                IUsingHandle<Range> linesRangeHandle; 
                setup.LinesReservation
                     .TryReserveRange(group.LinesCount, out linesRangeHandle);
 
                t = DateTime.Now;
                setup.Sorter.Sort(matrix, linesRangeHandle.Value);
                Out?.WriteLine($"[{logPrefix}] group sorting time: " +
                               $"{DateTime.Now - t}");

                t = DateTime.Now;
                using (var output = setup.IoService.OpenWrite(outputPath))
                    setup.SortedGroupWriter.Write(matrix, linesRangeHandle.Value, output);
                Out?.WriteLine($"[{logPrefix}] result writing time: " +
                               $"{DateTime.Now - t}");

                var outputSize = new FileInfo(outputPath).Length;
                Out?.WriteLine($"[{logPrefix}] output size: {outputSize}");

                var success = outputSize == new FileInfo(inputPath).Length;
                if (success)
                {
                    t = DateTime.Now;
                    success = Checker.IsSorted(outputPath);
                    Out?.WriteLine($"[{logPrefix}] sorting check time: " +
                                   $"{DateTime.Now - t}");
                }

                Out?.WriteLine();
                return success;
            };

            // private Func<IGroupInfo, string, string, string, bool> Ignore(
            //         Func<IGroupInfo, string, string, string, bool> _) =>
            //     (__, ___, ____, _____) => true;
        }
    }
}
