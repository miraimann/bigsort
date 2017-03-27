using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bigsort2.Contracts;
using Bigsort2.Implementation;
using Moq;
using NUnit.Framework;

namespace Bigsort2.Tests
{
    [TestFixture]
    public partial class GrouperTests
    {
        [TestCaseSource(nameof(Cases))]
        public void Test(TestCase testCase)
        {
            const string
                inputPath = "ZZZZZzzzzZzZZzzzZZZzzz",
                partsDirectory = "VVvvvVvVVVVvvvVvV",
                tempDirectory = "OOOOOooooOOOOooooo";        

            var ioServiceMock = new Mock<IIoService>();
            var configMock = new Mock<IConfig>();

            var inputStream = new MemoryStream(testCase.Source);
            var readerMock = new Mock<IReader>();
            readerMock
                .Setup(o => o.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((byte[] buff, int offset, int count) =>
                        inputStream.Read(buff, offset, count));

            readerMock
                .Setup(o => o.Dispose())
                .Callback(() => inputStream.Dispose());

            ioServiceMock
                .Setup(o => o.OpenRead(inputPath))
                .Returns(readerMock.Object);

            var realResult = new Dictionary<string, byte[]>();

            ioServiceMock
                .Setup(o => o.OpenWrite(It.IsAny<string>()))
                .Returns((string name) =>
                {
                    var stream = new MemoryStream();
                    var writerMock = new Mock<IWriter>();
                    writerMock
                        .Setup(o => o.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                        .Callback((byte[] buff, int offset, int count) =>
                                stream.Write(buff, offset, count));

                    writerMock
                        .Setup(o => o.Dispose())
                        .Callback(() =>
                        {
                            realResult.Add(name, stream.ToArray());
                            stream.Close();
                        });

                    return writerMock.Object;
                });

            configMock
                .SetupGet(o => o.BufferSize)
                .Returns(testCase.BufferSize);

            configMock
                .SetupGet(o => o.PartsDirectory)
                .Returns(partsDirectory);

            ioServiceMock
                .SetupGet(o => o.TempDirectory)
                .Returns(tempDirectory);
            
            var grouper = new Grouper(
                ioServiceMock.Object,
                configMock.Object);

            grouper.SplitToGroups(inputPath);
            
            CollectionAssert.AreEquivalent(
                testCase.ExpectedResult,
                realResult);
        }

        public class TestCase
        {
            private readonly string _name;
            public TestCase(string name)
            {
                _name = name;
            }

            public byte[] Source { get; set; }
            public int BufferSize { get; set; }
            public IDictionary<string, byte[]> ExpectedResult { get; set; }

            public override string ToString() =>
                _name;
        }

        public static IEnumerable<TestCase> Cases =>
            new[]
                {
                    Cases1
                }
                .Aggregate(Enumerable.Concat);

        private static byte[] BytesOf(string[] lines, bool withEndLines) =>
            string.Join(withEndLines ? Environment.NewLine : string.Empty, lines)
                  .Select(o => (byte) o)
                  .Concat(withEndLines && lines.Any() 
                            ? Environment.NewLine.Select(o => (byte) o)
                            : Enumerable.Empty<byte>())
                  .ToArray();

        private static string s(params sbyte[] numbers) =>
            new string(numbers.Select(o => (char) o).ToArray());

        private static string id(string key) =>
            (key[0]*byte.MaxValue + key[1]).ToString("00000");
    }
}
