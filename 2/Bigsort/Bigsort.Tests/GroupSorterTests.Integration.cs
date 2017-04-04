using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bigsort.Contracts;
using Bigsort.Implementation;
using Bigsort.Tools.SortedFileChecker;
using Moq;
using NUnit.Framework;
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
                BufferSize = 32*1024,
                MaxMemoryForLines = 64*1024*1024;

            private Setup<byte> _sorterByByteSetup;
            private Setup<uint> _sorterByUInt32Setup;
            private Setup<ulong> _sorterByUInt64Setup;

            private class Setup<T>
                where T : IEquatable<T>
                , IComparable<T>
            {
                public Mock<IConfig> ConfigMock;

                public IDisposableValueMaker DisposableValueMaker;
                public ILinesReservation<T> LinesReservation;
                public ILinesIndexesExtractor LinesIndexesExtractor;
                public ISegmentService<T> SegmentService;
                public ISortingSegmentsSupplier SegmentsSupplier;
                public IGroupBytesLoader GroupLoader;
                public IIoService IoService;
                public IBuffersPool BuffersPool;
                public ISortedGroupWriter SortedGroupWriter;
                public IGroupSorter Sorter;

                public Setup(ISegmentService<T> segmentService)
                {
                    SegmentService = segmentService;

                    ConfigMock = new Mock<IConfig>();
                    ConfigMock
                        .SetupGet(o => o.IsLittleEndian)
                        .Returns(BitConverter.IsLittleEndian);

                    ConfigMock
                        .SetupGet(o => o.GroupBufferRowReadingEnsurance)
                        .Returns(GroupBufferRowReadingEnsurance);

                    ConfigMock
                        .SetupGet(o => o.BufferSize)
                        .Returns(BufferSize);

                    ConfigMock
                        .SetupGet(o => o.MaxMemoryForLines)
                        .Returns(MaxMemoryForLines);

                    DisposableValueMaker = new DisposableValueMaker();
                    LinesReservation = new LinesReservation<T>(
                        DisposableValueMaker,
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

                    BuffersPool = new BuffersPool(
                        DisposableValueMaker,
                        ConfigMock.Object);

                    IoService = new IoService(BuffersPool);
                    GroupLoader = new GroupBytesLoader(
                        BuffersPool,
                        IoService,
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
                    new UInt32SegmentService(BitConverter.IsLittleEndian));

                _sorterByUInt64Setup = new Setup<ulong>(
                    new UInt64SegmentService(BitConverter.IsLittleEndian));
            }

            public void Test(
                int maxNumberLength,
                int maxStringLength,
                int linesCount,
                int buffSize,
                bool clear = true)
            {
                var runners = new[]
                {
                    RunnerFor(_sorterByByteSetup),
                    RunnerFor(_sorterByUInt32Setup),
                    RunnerFor(_sorterByUInt64Setup)
                };

                var subWorkingDirectories = Names
                    .Select(name => Path.Combine(WorkingDirectory, name))
                    .ToArray();

                var inputPath = subWorkingDirectories
                    .Select(dir => Path.Combine(dir, GroupId))
                    .ToArray();

                var outputPath = inputPath
                    .Select(origin => origin + ResultFileSufix)
                    .ToArray();

                if (!Directory.Exists(WorkingDirectory))
                     Directory.CreateDirectory(WorkingDirectory);
                
                Environment.CurrentDirectory = WorkingDirectory;
                foreach (var dir in subWorkingDirectories)
                    if (!Directory.Exists(dir))
                         Directory.CreateDirectory(dir);
                try
                {
                    var groupInfo = GroupGenerator
                        .Generate(GroupId,
                                  inputPath[ForByte],
                                  linesCount,
                                  maxNumberLength,
                                  maxStringLength);

                    File.Copy(inputPath[ForByte], inputPath[ForUInt32]);
                    File.Copy(inputPath[ForByte], inputPath[ForUInt64]);

                    Out?.WriteLine($"input size: {groupInfo.BytesCount}");

                    var testResult =
                        Names.Zip(inputPath, (logPrefix, input) => new { logPrefix, input })
                             .Zip(outputPath, (o, output) => new { o.logPrefix, o.input, output })
                             .Zip(runners, (o, runner) => runner(groupInfo, o.input, o.output, o.logPrefix))
                             .Zip(Names, (success, name) => new { success, name })
                             .ToArray();
                    
                    Assert.IsTrue(
                        testResult.All(o => o.success),
                        testResult.Where(o => !o.success)
                                  .Select(o => o.name)
                                  .Aggregate("failed = {", (acc, o) => $"{acc} {o}") + "}"
                                  );
                }
                finally
                {
                    if (clear && Directory.Exists(WorkingDirectory))
                        Directory.Delete(WorkingDirectory, true);
                }
            }

            private Func<IGroupInfo, string, string, string, bool> RunnerFor<T>(Setup<T> setup)
                where T : IEquatable<T>, IComparable<T> => (group, inputPath, outputPath, logPrefix) =>
                {
                    DateTime t = DateTime.Now;
                    var groupBytes =
                        setup.GroupLoader.LoadMatrix(
                            setup.GroupLoader.CalculateMatrixInfo(group));
                    
                    Out?.WriteLine($"[{logPrefix}] grop loading time: " +
                                   $"{DateTime.Now - t}");

                    setup.LinesReservation.Load();
                    setup.LinesReservation.TryReserveRange(group.LinesCount);

                    var linesRange = new Range(0, group.LinesCount);

                    t = DateTime.Now;
                    setup.Sorter.Sort(groupBytes, linesRange);
                    Out?.WriteLine($"[{logPrefix}] grop sorting time: " +
                                   $"{DateTime.Now - t}");

                    t = DateTime.Now;
                    using (var output = setup.IoService.OpenWrite(outputPath))
                        setup.SortedGroupWriter.Write(groupBytes, linesRange, output);
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

                    return success;
                };
        }
    }
}
