using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bigsort.Contracts;
using Bigsort.Implementation;
using Moq;

namespace Bigsort.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = "E:\\1Gb";
            var resultDir = "E:\\ODir";
            var configMock = new Mock<IConfig>();
            configMock
                .SetupGet(o => o.BufferSize)
                .Returns(32 * 1024);

            configMock
                .SetupGet(o => o.IsLittleEndian)
                .Returns(BitConverter.IsLittleEndian);

            var log = System.Console.Out;
            var disposableValueMaker = new UsingHandleMaker();
            var buffersPool = new BuffersPool(disposableValueMaker, configMock.Object);
            var ioService = new IoService(buffersPool);
            var taskQueueMaker = new TasksQueueMaker();
            var usingHandleMaker = new UsingHandleMaker();
            var buffersReaderMaker = new GrouperBuffersProviderMaker(
                buffersPool, ioService, usingHandleMaker);

            var grouper = new Grouper(
                //taskQueueMaker,
                //buffersPool,
                ioService,
                configMock.Object);

            var t = DateTime.Now;
            // grouper.SplitToGroups(path, resultDir);
            // log.WriteLine("grouping time: {0}", DateTime.Now - t);
            // log.WriteLine("result path: {0}", resultDir);

            // var expectedSize = new FileInfo(path).Length;
            // var resultSize = Directory
            //     .EnumerateFiles(resultDir)
            //     .Select(o => new FileInfo(o).Length)
            //     .Sum();
            // 
            // log.WriteLine("input file size: {0}", expectedSize);
            // log.WriteLine("group files size: {0}", resultSize);

            resultDir = "E:\\XDir";

            var asyncGrouper = new AsyncGrouper1(
                //buffersPool,
                buffersReaderMaker,
                taskQueueMaker,
                ioService,
                configMock.Object);

            t = DateTime.Now;
            //GC.TryStartNoGCRegion(4L*1024*1024);
            asyncGrouper.SplitToGroups(path, resultDir);
            //GC.EndNoGCRegion();
            log.WriteLine("grouping time: {0}", DateTime.Now - t);
            log.WriteLine("result path: {0}", resultDir);

            var expectedSize = new FileInfo(path).Length;
            var resultSize = Directory
                .EnumerateFiles(resultDir)
                .Select(o => new FileInfo(o).Length)
                .Sum();

            log.WriteLine("input file size: {0}", expectedSize);
            log.WriteLine("group files size: {0}", resultSize);

            System.Console.ReadKey();
        }
    }
}
