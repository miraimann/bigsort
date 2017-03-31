//using System;
//using Bigsort.Contracts;
//using Bigsort.Implementation;
//using Bigsort.Tools.TestFileGenerator;
//using Moq;
//using NUnit.Framework;

//namespace Bigsort.Tests
//{
//    // ReSharper disable once InconsistentNaming
//    public partial class Grouper_255_255x255Tests
//    {
//        [TestCase("1_Mb", "[1-32].[0-128]", "E:\\1Mb", 32 * 1025
//            //, Ignore = "for hands run only"
//            )]

//        [TestCase("1_Gb", "[1-32].[0-128]", "E:\\1Gb", 32 * 1025
//            //, Ignore = "for hands run only"
//            )]

//        public void IntegrationTest(
//            string size,
//            string settings,
//            string path,
//            int buffSize)
//        {
//            var configMock = new Mock<IConfig>();
//            configMock
//                .SetupGet(o => o.BufferSize)
//                .Returns(buffSize);

//            configMock
//                .SetupGet(o => o.PartsDirectory)
//                .Returns("result");

//            var log = TestContext.Out;
//            var poolMaker = new PoolMaker();
//            var buffersPool = new BuffersPool(poolMaker, configMock.Object);
//            var ioService = new IoService(buffersPool);
//            var grouper = new Grouper_255_255x255(ioService, configMock.Object);

//            var t = DateTime.Now;
//            Generator.Generate(size, settings, path);
//            log.WriteLine("generation time: {0}", DateTime.Now - t);

//            t = DateTime.Now;
//            var resultDiretory = grouper.SplitToGroups(path);
//            log.WriteLine("grouping time: {0}", DateTime.Now - t);
//            log.WriteLine("result path: {0}", resultDiretory);
//        }
//    }
//}
