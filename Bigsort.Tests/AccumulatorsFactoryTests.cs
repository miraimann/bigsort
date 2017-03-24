using System;
using System.Collections.Generic;
using System.Linq;
using Bigsort.Contracts;
using Bigsort.Implementation;
using Moq;
using NUnit.Framework;

namespace Bigsort.Tests
{
    [TestFixture]
    public class AccumulatorsFactoryTests
    {
        private IPoolMaker _poolMaker;
        private IAccumulatorsFactory _accumulatorsFactory;
        private Mock<IConfig> _config;
        private IIoService _ioService;
        private Mock<IIoService> _ioServiceMock;
        private IBytesConvertersFactory _bytesConvertersFactory;
        private Dictionary<string, byte[]> _streamsContent;

        [SetUp]
        public void Setup()
        {
            _config = new Mock<IConfig>();

            _config.SetupGet(o => o.BytesEnumeratingBufferSize)
                   .Returns(1024 * 1024);

            _ioService = new IoService(_config.Object);
            _ioServiceMock = new Mock<IIoService>();
            _ioServiceMock
                .SetupGet(o => o.TempDirectory)
                .Returns("ZZZzzzzZZZZzzZzZzzz");

            _streamsContent = new Dictionary<string, byte[]>();

            _ioServiceMock
                .Setup(o => o.OpenRead(It.IsAny<string>()))
                .Returns((string path) =>
                {
                    var stream = _ioService.CreateInMemory();
                    var buff = _streamsContent[path];
                    stream.Write(buff, 0, buff.Length);
                    stream.Position = 0;
                    return stream;
                });

            _ioServiceMock
                .Setup(o => o.OpenWrite(It.IsAny<string>()))
                .Returns((string path) =>
                    new DummyFileWritingStream(_streamsContent, path));
            
            _poolMaker = new PoolMaker();
            _bytesConvertersFactory = new BytesConvertersFactory();
            _accumulatorsFactory = new AccumulatorsFactory(
                _poolMaker,
                _ioServiceMock.Object,
                _bytesConvertersFactory,
                _config.Object);
        }

        [Test]
        public void AccumulatorCountTest(
            [Values(1, 2, 3, 10, 12)] int fragmentLength,
            [Values(0, 1, 2, 3, 5, 9, 11, 100, 1234)] int itemsCount)
        {
            _config.SetupGet(o => o.IntsAccumulatorFragmentSize)
                   .Returns(fragmentLength);

            var accumulator = _accumulatorsFactory
                .CreateForInt();

            Assert.AreEqual(0, accumulator.Count);

            for (int i = 0; i < itemsCount; i++)
                accumulator.Add(i);

            Assert.AreEqual(itemsCount, accumulator.Count);
        }

        [Test]
        public void AccumulatorItemsTest(
            [Values(1, 2, 3, 10, 12)] int fragmentLength,
            [Values(0, 1, 2, 3, 5, 9, 11, 100, 1234)] int itemsCount)
        {
            _config.SetupGet(o => o.IntsAccumulatorFragmentSize)
                   .Returns(fragmentLength);

            var accumulator = _accumulatorsFactory
                .CreateForInt();
            
            var random = new Random();
            var items = Enumerable
                .Range(0, itemsCount)
                .Select(_ => random.Next())
                .ToArray();

            for (int i = 0; i < itemsCount; i++)
                accumulator.Add(items[i]);
            
            CollectionAssert.AreEqual(items, accumulator);
            for (int i = 0; i < itemsCount; i++)
                Assert.AreEqual(items[i], accumulator[i]);
        }

        [Test]
        public void CacheableAccumulatorForIntCountTest(
            [Values(1, 2, 3, 10, 12)] int fragmentLength,
            [Values(0, 1, 2, 3, 9, 11, 100, 1234)] int itemsCount)
        {
            _config.SetupGet(o => o.MaxCollectionSize)
                   .Returns(fragmentLength * sizeof(int));

            var accumulator = _accumulatorsFactory
                .CreateCacheableForInt();

            Assert.AreEqual(0, accumulator.Count);

            for (int i = 0; i < itemsCount; i++)
                accumulator.Add(i);

            Assert.AreEqual(itemsCount, accumulator.Count);
        }

        [Test]
        public void CacheableAccumulatorForIntItemsTest(
            [Values(1, 2, 3, 10, 12)] int fragmentLength,
            [Values(0, 1, 2, 3, 9, 11, 100, 1234)] int itemsCount)
        {
            _config.SetupGet(o => o.MaxCollectionSize)
                   .Returns(fragmentLength * sizeof(int));

            var accumulator = _accumulatorsFactory
                .CreateCacheableForInt();

            var random = new Random();
            var items = Enumerable
                .Range(0, itemsCount)
                .Select(_ => random.Next())
                .ToArray();

            for (int i = 0; i < itemsCount; i++)
                accumulator.Add(items[i]);

            CollectionAssert.AreEqual(items, accumulator);
            for (int i = 0; i < itemsCount; i++)
                Assert.AreEqual(items[i], accumulator[i]);
        }

        [Test]
        public void CacheableAccumulatorForLongItemsTest(
            [Values(1, 2, 3, 10, 12)] int fragmentLength,
            [Values(0, 1, 2, 3, 9, 11, 100, 1234)] int itemsCount)
        {
            _config.SetupGet(o => o.MaxCollectionSize)
                   .Returns(fragmentLength * sizeof(long));

            var accumulator = _accumulatorsFactory
                .CreateCacheableForLong();

            var random = new Random();
            var items = Enumerable
                .Range(0, itemsCount)
                .Select(_ => random.Next())
                .ToArray();

            for (int i = 0; i < itemsCount; i++)
                accumulator.Add(items[i]);

            CollectionAssert.AreEqual(items, accumulator);
            for (int i = 0; i < itemsCount; i++)
                Assert.AreEqual(items[i], accumulator[i]);
        }

        [Test]
        public void CacheableAccumulatorForLongCountTest(
            [Values(1, 2, 3, 10, 12)] int fragmentLength,
            [Values(0, 1, 2, 3, 9, 11, 100, 1234)] int itemsCount)
        {
            _config.SetupGet(o => o.MaxCollectionSize)
                   .Returns(fragmentLength * sizeof(long));

            var accumulator = _accumulatorsFactory
                .CreateCacheableForLong();

            Assert.AreEqual(0, accumulator.Count);

            for (int i = 0; i < itemsCount; i++)
                accumulator.Add(i);

            Assert.AreEqual(itemsCount, accumulator.Count);
        }
    }
}
