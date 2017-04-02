using System;
using System.IO;
using System.Linq;
using Bigsort.Contracts;
using Bigsort.Implementation;
using Bigsort.Tools.TestFileGenerator;
using Moq;
using NUnit.Framework;

namespace Bigsort.Tests
{
    public partial class GrouperTests
    {
        public const string UseExistendFile = "X"; 

        [TestCase("1_Mb", "[1-32].[0-128]", "E:\\1Mb", 32*1025
             , Ignore = "for hands run only"
         )]

        [TestCase("1_Gb", "[1-32].[0-128]", "E:\\1Gb", 32*1025
             , Ignore = "for hands run only"
         )]

        [TestCase(UseExistendFile, "[1-32].[0-128]", "E:\\1Gb", 32 * 1025
            //, Ignore = "for hands run only"
         )]

        [TestCase("10_Gb", "[1-32].[0-128]", "E:\\10Gb", 32 * 1025
         , Ignore = "for hands run only"
         )]

        public void IntegrationTest(
            string size,
            string settings,
            string path,
            int buffSize)
        {
            var resultDir = $"E:\\{Path.GetFileName(path)}Dir";
            var configMock = new Mock<IConfig>();
            configMock
                .SetupGet(o => o.BufferSize)
                .Returns(buffSize);

            var log = TestContext.Out;
            var disposableValueMaker = new DisposableValueMaker();
            var buffersPool = new BuffersPool(disposableValueMaker, configMock.Object);
            var ioService = new IoService(buffersPool);
            var grouper = new Grouper(ioService, configMock.Object);

            try
            {
                DateTime t;
                if (size != UseExistendFile)
                {
                    t = DateTime.Now;
                    Generator.Generate(size, settings, path);
                    log.WriteLine("generation time: {0}", DateTime.Now - t);
                }
                
                // t = DateTime.Now;
                // grouper.SplitToGroups(path, resultDir);
                // log.WriteLine("grouping time: {0}", DateTime.Now - t);
                // log.WriteLine("result path: {0}", resultDir);

                var expectedSize = new FileInfo(path).Length;
                var resultSize = Directory
                    .EnumerateFiles(resultDir)
                    .Select(o => new FileInfo(o).Length)
                    .Sum();

                log.WriteLine("input file size: {0}", expectedSize);
                log.WriteLine("group files size: {0}", resultSize);

                Assert.AreEqual(expectedSize, resultSize);
            }
            finally
            {
                // File.Delete(path);
                // Directory.Delete(resultDir, true);
            }
        }
    }
}
