//using System;
//using System.IO;
//using System.Linq;
//using Bigsort.Contracts;
//using Bigsort.Implementation;
//using Moq;
//using NUnit.Framework;
//using static NUnit.Framework.TestContext;

//namespace Bigsort.Tests
//{
//    public class GroupBytesLoaderTests
//    {
//        public class Integration
//        {
//            public const string
//                WorkingDirectory = "E:\\GroupBytesLoaderTests",
//                GroupId = "XX";

//            private const int BufferSize = 32*1024,
//                GroupBufferRowReadingEnsurance = 7;

//            [TestCase(128, 225, 10000, 32 * 1000, false
//                //, Ignore = "for hands run only"
//            )]
//            public void Test(
//                int maxNumberLength,
//                int maxStringLength,
//                int linesCount,
//                int buffSize,
//                bool clear = true)
//            {
//                var configMock = new Mock<IConfig>();
//                configMock
//                    .SetupGet(o => o.BufferSize)
//                    .Returns(BufferSize);

//                configMock
//                    .SetupGet(o => o.GroupBufferRowReadingEnsurance)
//                    .Returns(GroupBufferRowReadingEnsurance);

//                var disposableMaker =
//                    new UsingHandleMaker();

//                var pool = new BuffersPool(
//                    disposableMaker,
//                    configMock.Object);

//                var ioService = new IoService(pool);

//                var loader = new GroupBytesLoader(
//                    pool, ioService, configMock.Object);

//                try
//                {
//                    if (!Directory.Exists(WorkingDirectory))
//                        Directory.CreateDirectory(WorkingDirectory);

//                    Environment.CurrentDirectory = WorkingDirectory;

//                    var loadedFileName = GroupId + "_loaded";
//                    var originPath = Path.Combine(WorkingDirectory, GroupId);
//                    var seed = GroupGenerator.Generate(
//                        GroupId, originPath, linesCount, maxNumberLength, maxStringLength);

//                    var info = loader.CalculateMatrixInfo(seed);
//                    var group = loader.LoadMatrix(info);

//                    using (var writer = File.OpenWrite(GroupId + "_loaded"))
//                    {
//                        for (var i = 0; i < info.RowsCount - 1; i++)
//                            writer.Write(group.Rows[i], 0, group.RowLength);
//                        writer.Write(group.Rows.Last(), 0, 
//                            group.BytesCount % group.RowLength);
//                    }

//                    var originSize = new FileInfo(GroupId).Length;
//                    var loadedSize = new FileInfo(loadedFileName).Length;

//                    Out?.WriteLine($"generated size: {originSize}");
//                    Out?.WriteLine($"loaded size: {loadedSize}");
//                    Out?.WriteLine();

//                    Assert.AreEqual(originSize, loadedSize);

//                    using (var origin = File.OpenRead(GroupId))
//                    using (var loaded = File.OpenRead(loadedFileName))
//                    {
//                        while (origin.Position != origin.Length)
//                            Assert.AreEqual(
//                                origin.ReadByte(), 
//                                loaded.ReadByte());
//                    }
//                }
//                finally
//                {
//                    if (clear && Directory.Exists(WorkingDirectory))
//                        Directory.Delete(WorkingDirectory, true);
//                }

//            }
//        }
//    }
//}
