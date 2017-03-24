using Bigsort.Contracts;
using Bigsort.Implementation;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace Bigsort.Tests
{
    [TestFixture]
    public class LinesSorterTests
    {
        private Mock<IConfig> _config;
        private MemoryStream _inputStream, _outputStream;
        private Mock<IIoService> _ioServiceMock;
        private IIoService _ioService;
        private ILinesIndexator _linesIndexator;
        private IResultWriter _resultWriter;
        private ILinesSorter _linesSorter;
        private ISortersMaker _sortersMaker;
        private IPoolMaker _poolMaker;
        private IIndexedInputService _indexedInputService;
        private IAccumulatorsFactory _accumulatorsFactory;
        private IBytesConvertersFactory _bytesConvertersFactory;
        private Dictionary<string, byte[]> _streamsContent;

        [SetUp]
        public void SetUp()
        {
            _inputStream = new MemoryStream();
            _outputStream = new MemoryStream();

            _config = new Mock<IConfig>();

            _config.SetupGet(o => o.BytesEnumeratingBufferSize)
                   .Returns(1024);
            _config.SetupGet(o => o.ResultWriterBufferSize)
                   .Returns(1024);
            _config.SetupGet(o => o.EndLine)
                   .Returns(Consts.EndLineBytes);
            _config.SetupGet(o => o.Dot)
                   .Returns(Consts.Dot);
            _config.SetupGet(o => o.MaxCollectionSize)
                   .Returns(16);
            _config.SetupGet(o => o.IntsAccumulatorFragmentSize)
                   .Returns(2);

            _ioService = new IoService(_config.Object);
            _ioServiceMock = new Mock<IIoService>();

            _ioServiceMock
                .SetupGet(o => o.TempDirectory)
                .Returns("ZZZzzzzZZZZzzZzZzzz");
            
            _ioServiceMock
                .Setup(o => o.EnumeratesBytesOf(Consts.InputFilePath))
                .Returns(_inputStream.ToArray);

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

            _ioServiceMock
                .Setup(o => o.OpenRead(Consts.InputFilePath))
                .Returns(_ioService.Adapt(_inputStream));

            _ioServiceMock
                .Setup(o => o.OpenWrite(Consts.OutputFilePath))
                .Returns(_ioService.Adapt(_outputStream));

            _linesIndexator = new LinesIndexator(_config.Object);
            _resultWriter = new ResultWriter(_config.Object);
            _poolMaker = new PoolMaker();
            _bytesConvertersFactory = new BytesConvertersFactory();
            _accumulatorsFactory = new AccumulatorsFactory(
                _poolMaker, 
                _ioServiceMock.Object,
                _bytesConvertersFactory,
                _config.Object);
            
            _sortersMaker = new SortersMaker(_accumulatorsFactory, _poolMaker);
            _indexedInputService = new IndexedInputService(_config.Object);
            _linesSorter = new LinesSorter(
                _ioServiceMock.Object,
                _linesIndexator,
                _sortersMaker,
                _indexedInputService,
                _accumulatorsFactory,
                _resultWriter);
        }

        [TestCaseSource(nameof(Seeds))]
        public void Test(InputOutputTestCase.Seed seed)
        {
            var testCase = InputOutputTestCase.Parse(seed);
            _inputStream.Write(testCase.InputFileContent, 0,
                testCase.InputFileContent.Length);
            _inputStream.Position = 0;

            _linesSorter.Sort(Consts.InputFilePath, Consts.OutputFilePath);

            CollectionAssert.AreEqual(
                testCase.ExpectedOutputFileContent,
                _outputStream.ToArray());
        }

        public static IEnumerable<InputOutputTestCase.Seed> Seeds
        {
            get
            {
                #region 0 - 99

                yield return new InputOutputTestCase.Seed("0")
                {
                    InputFileContent = new string[0],
                    ExpectedOutputFileContent = new string[0]
                };

                yield return new InputOutputTestCase.Seed("1")
                {
                    InputFileContent = new[]
                    {
                        "6574386. aaab"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "6574386. aaab"
                    }
                };

                yield return new InputOutputTestCase.Seed("2")
                {
                    InputFileContent = new[]
                    {
                        "6574386. aaab" + Consts.EndLine,
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "6574386. aaab"
                    }
                };

                yield return new InputOutputTestCase.Seed("3")
                {
                    InputFileContent = new[]
                    {
                        "0.a",
                        "0.b"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "0.b"
                    }
                };

                yield return new InputOutputTestCase.Seed("4")
                {
                    InputFileContent = new[]
                    {
                        "0.b",
                        "0.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "0.b"
                    }
                };

                yield return new InputOutputTestCase.Seed("5")
                {
                    InputFileContent = new[]
                    {
                        "1.a",
                        "0.b"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.a",
                        "0.b"
                    }
                };

                yield return new InputOutputTestCase.Seed("6")
                {
                    InputFileContent = new[]
                    {
                        "0.b",
                        "1.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.a",
                        "0.b"
                    }
                };

                yield return new InputOutputTestCase.Seed("7")
                {
                    InputFileContent = new[]
                    {
                        "1743659374659836582639.a",
                        "065413784378.b"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1743659374659836582639.a",
                        "065413784378.b"
                    }
                };

                yield return new InputOutputTestCase.Seed("8")
                {
                    InputFileContent = new[]
                    {
                        "065413784378.b",
                        "1743659374659836582639.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1743659374659836582639.a",
                        "065413784378.b"
                    }
                };

                yield return new InputOutputTestCase.Seed("9")
                {
                    InputFileContent = new[]
                    {
                        "0.a",
                        "1.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "1.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("10")
                {
                    InputFileContent = new[]
                    {
                        "1.a",
                        "0.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "1.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("11")
                {
                    InputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("12")
                {
                    InputFileContent = new[]
                    {
                        "1.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("13")
                {
                    InputFileContent = new[]
                    {
                        "0.abcdefght",
                        "1.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.abcdefght",
                        "1.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("14")
                {
                    InputFileContent = new[]
                    {
                        "1.abcdefght",
                        "0.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.abcdefght",
                        "1.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("15")
                {
                    InputFileContent = new[]
                    {
                        "0.a",
                        "01.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "01.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("16")
                {
                    InputFileContent = new[]
                    {
                        "01.a",
                        "0.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "01.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("17")
                {
                    InputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "01.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "01.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("18")
                {
                    InputFileContent = new[]
                    {
                        "01.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "01.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("19")
                {
                    InputFileContent = new[]
                    {
                        "0.abcdefght",
                        "01.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.abcdefght",
                        "01.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("20")
                {
                    InputFileContent = new[]
                    {
                        "01.abcdefght",
                        "0.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.abcdefght",
                        "01.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("21")
                {
                    InputFileContent = new[]
                    {
                        "0.a",
                        "00000000000000000001.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "00000000000000000001.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("22")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000001.a",
                        "0.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "00000000000000000001.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("23")
                {
                    InputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000001.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000001.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("24")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000001.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000001.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("25")
                {
                    InputFileContent = new[]
                    {
                        "0.abcdefght",
                        "00000000000000000001.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.abcdefght",
                        "00000000000000000001.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("26")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000001.abcdefght",
                        "0.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.abcdefght",
                        "00000000000000000001.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("27")
                {
                    InputFileContent = new[]
                    {
                        "00.a",
                        "1.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00.a",
                        "1.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("28")
                {
                    InputFileContent = new[]
                    {
                        "1.a",
                        "00.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00.a",
                        "1.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("29")
                {
                    InputFileContent = new[]
                    {
                        "00.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("30")
                {
                    InputFileContent = new[]
                    {
                        "1.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("31")
                {
                    InputFileContent = new[]
                    {
                        "00.abcdefght",
                        "1.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00.abcdefght",
                        "1.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("32")
                {
                    InputFileContent = new[]
                    {
                        "1.abcdefght",
                        "00.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00.abcdefght",
                        "1.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("33")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000000.a",
                        "1.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.a",
                        "1.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("34")
                {
                    InputFileContent = new[]
                    {
                        "1.a",
                        "000000000000000000.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.a",
                        "1.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("35")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("36")
                {
                    InputFileContent = new[]
                    {
                        "1.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("37")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000000.abcdefght",
                        "1.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.abcdefght",
                        "1.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("38")
                {
                    InputFileContent = new[]
                    {
                        "1.abcdefght",
                        "000000000000000000.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.abcdefght",
                        "1.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("39")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000000.a",
                        "0000000001.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.a",
                        "0000000001.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("40")
                {
                    InputFileContent = new[]
                    {
                        "0000000001.a",
                        "000000000000000000.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.a",
                        "0000000001.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("41")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0000000001.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0000000001.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("42")
                {
                    InputFileContent = new[]
                    {
                        "0000000001.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0000000001.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("43")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000000.abcdefght",
                        "0000000001.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.abcdefght",
                        "0000000001.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("44")
                {
                    InputFileContent = new[]
                    {
                        "0000000001.abcdefght",
                        "000000000000000000.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.abcdefght",
                        "0000000001.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("45")
                {
                    InputFileContent = new[]
                    {
                        "2.a",
                        "3.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.a",
                        "3.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("46")
                {
                    InputFileContent = new[]
                    {
                        "3.a",
                        "2.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.a",
                        "3.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("47")
                {
                    InputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "3.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "3.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("48")
                {
                    InputFileContent = new[]
                    {
                        "3.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "3.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("49")
                {
                    InputFileContent = new[]
                    {
                        "2.abcdefght",
                        "3.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.abcdefght",
                        "3.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("50")
                {
                    InputFileContent = new[]
                    {
                        "3.abcdefght",
                        "2.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.abcdefght",
                        "3.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("51")
                {
                    InputFileContent = new[]
                    {
                        "2.a",
                        "03.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.a",
                        "03.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("52")
                {
                    InputFileContent = new[]
                    {
                        "03.a",
                        "2.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.a",
                        "03.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("53")
                {
                    InputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "03.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "03.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("54")
                {
                    InputFileContent = new[]
                    {
                        "03.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "03.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("55")
                {
                    InputFileContent = new[]
                    {
                        "2.abcdefght",
                        "03.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.abcdefght",
                        "03.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("56")
                {
                    InputFileContent = new[]
                    {
                        "03.abcdefght",
                        "2.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.abcdefght",
                        "03.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("57")
                {
                    InputFileContent = new[]
                    {
                        "2.a",
                        "00000000000000000003.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.a",
                        "00000000000000000003.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("58")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000003.a",
                        "2.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.a",
                        "00000000000000000003.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("59")
                {
                    InputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000003.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000003.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("60")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000003.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000003.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("61")
                {
                    InputFileContent = new[]
                    {
                        "2.abcdefght",
                        "00000000000000000003.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.abcdefght",
                        "00000000000000000003.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("62")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000003.abcdefght",
                        "2.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.abcdefght",
                        "00000000000000000003.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("63")
                {
                    InputFileContent = new[]
                    {
                        "02.a",
                        "3.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "02.a",
                        "3.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("64")
                {
                    InputFileContent = new[]
                    {
                        "3.a",
                        "02.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "02.a",
                        "3.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("65")
                {
                    InputFileContent = new[]
                    {
                        "02.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "3.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "02.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "3.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("66")
                {
                    InputFileContent = new[]
                    {
                        "3.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "02.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "02.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "3.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("67")
                {
                    InputFileContent = new[]
                    {
                        "02.abcdefght",
                        "3.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "02.abcdefght",
                        "3.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("68")
                {
                    InputFileContent = new[]
                    {
                        "3.abcdefght",
                        "02.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "02.abcdefght",
                        "3.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("69")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000002.a",
                        "3.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.a",
                        "3.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("70")
                {
                    InputFileContent = new[]
                    {
                        "3.a",
                        "000000000000000002.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.a",
                        "3.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("71")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "3.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "3.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("72")
                {
                    InputFileContent = new[]
                    {
                        "3.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "3.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("73")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000002.abcdefght",
                        "3.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.abcdefght",
                        "3.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("74")
                {
                    InputFileContent = new[]
                    {
                        "3.abcdefght",
                        "000000000000000002.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.abcdefght",
                        "3.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("75")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000002.a",
                        "0000000003.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.a",
                        "0000000003.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("76")
                {
                    InputFileContent = new[]
                    {
                        "0000000003.a",
                        "000000000000000002.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.a",
                        "0000000003.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("77")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0000000003.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0000000003.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("78")
                {
                    InputFileContent = new[]
                    {
                        "0000000003.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0000000003.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("79")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000002.abcdefght",
                        "0000000003.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.abcdefght",
                        "0000000003.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("80")
                {
                    InputFileContent = new[]
                    {
                        "0000000003.abcdefght",
                        "000000000000000002.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.abcdefght",
                        "0000000003.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("81")
                {
                    InputFileContent = new[]
                    {
                        "6574386. aaab",
                        "3762548273691. bbbbsjldjasl",
                        "8.",
                        "8943659. uiuiui"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "8.",
                        "6574386. aaab",
                        "3762548273691. bbbbsjldjasl",
                        "8943659. uiuiui"
                    }
                };

                yield return new InputOutputTestCase.Seed("82")
                {
                    InputFileContent = new[]
                    {
                        "6574386. aaab",
                        "8.",
                        "3762548273691. bbbbsjldjasl",
                        "8943659. uiuiui"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "8.",
                        "6574386. aaab",
                        "3762548273691. bbbbsjldjasl",
                        "8943659. uiuiui"
                    }
                };

                yield return new InputOutputTestCase.Seed("83")
                {
                    InputFileContent = new[]
                    {
                        "6789.a",
                        "0.b"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "6789.a",
                        "0.b"
                    }
                };

                yield return new InputOutputTestCase.Seed("84")
                {
                    InputFileContent = new[]
                    {
                        "0.b",
                        "6789.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "6789.a",
                        "0.b"
                    }
                };

                yield return new InputOutputTestCase.Seed("85")
                {
                    InputFileContent = new[]
                    {
                        "1743659374659836582639.a",
                        "065413784378.b"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1743659374659836582639.a",
                        "065413784378.b"
                    }
                };

                yield return new InputOutputTestCase.Seed("86")
                {
                    InputFileContent = new[]
                    {
                        "065413784378.b",
                        "1743659374659836582639.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1743659374659836582639.a",
                        "065413784378.b"
                    }
                };

                yield return new InputOutputTestCase.Seed("87")
                {
                    InputFileContent = new[]
                    {
                        "0.a",
                        "6789.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "6789.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("88")
                {
                    InputFileContent = new[]
                    {
                        "6789.a",
                        "0.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "6789.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("89")
                {
                    InputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "6789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "6789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("90")
                {
                    InputFileContent = new[]
                    {
                        "6789.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "6789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("91")
                {
                    InputFileContent = new[]
                    {
                        "0.abcdefght",
                        "6789.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.abcdefght",
                        "6789.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("92")
                {
                    InputFileContent = new[]
                    {
                        "6789.abcdefght",
                        "0.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.abcdefght",
                        "6789.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("93")
                {
                    InputFileContent = new[]
                    {
                        "0.a",
                        "06789.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "06789.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("94")
                {
                    InputFileContent = new[]
                    {
                        "06789.a",
                        "0.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "06789.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("95")
                {
                    InputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "06789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "06789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("96")
                {
                    InputFileContent = new[]
                    {
                        "06789.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "06789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("97")
                {
                    InputFileContent = new[]
                    {
                        "0.abcdefght",
                        "06789.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.abcdefght",
                        "06789.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("98")
                {
                    InputFileContent = new[]
                    {
                        "06789.abcdefght",
                        "0.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.abcdefght",
                        "06789.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("99")
                {
                    InputFileContent = new[]
                    {
                        "0.a",
                        "00000000000000000006789.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "00000000000000000006789.a"
                    }
                };

                #endregion

                #region 100 - 199

                yield return new InputOutputTestCase.Seed("100")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000006789.a",
                        "0.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "00000000000000000006789.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("101")
                {
                    InputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000006789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000006789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("102")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000006789.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000006789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("103")
                {
                    InputFileContent = new[]
                    {
                        "0.abcdefght",
                        "00000000000000000006789.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.abcdefght",
                        "00000000000000000006789.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("104")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000006789.abcdefght",
                        "0.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.abcdefght",
                        "00000000000000000006789.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("105")
                {
                    InputFileContent = new[]
                    {
                        "00.a",
                        "6789.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00.a",
                        "6789.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("106")
                {
                    InputFileContent = new[]
                    {
                        "6789.a",
                        "00.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00.a",
                        "6789.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("107")
                {
                    InputFileContent = new[]
                    {
                        "00.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "6789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "6789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("108")
                {
                    InputFileContent = new[]
                    {
                        "6789.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "6789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("109")
                {
                    InputFileContent = new[]
                    {
                        "00.abcdefght",
                        "6789.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00.abcdefght",
                        "6789.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("110")
                {
                    InputFileContent = new[]
                    {
                        "6789.abcdefght",
                        "00.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00.abcdefght",
                        "6789.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("111")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000000.a",
                        "6789.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.a",
                        "6789.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("112")
                {
                    InputFileContent = new[]
                    {
                        "6789.a",
                        "000000000000000000.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.a",
                        "6789.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("113")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "6789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "6789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("114")
                {
                    InputFileContent = new[]
                    {
                        "6789.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "6789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("115")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000000.abcdefght",
                        "6789.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.abcdefght",
                        "6789.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("116")
                {
                    InputFileContent = new[]
                    {
                        "6789.abcdefght",
                        "000000000000000000.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.abcdefght",
                        "6789.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("117")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000000.a",
                        "0000000006789.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.a",
                        "0000000006789.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("118")
                {
                    InputFileContent = new[]
                    {
                        "0000000006789.a",
                        "000000000000000000.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.a",
                        "0000000006789.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("119")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0000000006789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0000000006789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("120")
                {
                    InputFileContent = new[]
                    {
                        "0000000006789.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0000000006789.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("121")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000000.abcdefght",
                        "0000000006789.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.abcdefght",
                        "0000000006789.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("122")
                {
                    InputFileContent = new[]
                    {
                        "0000000006789.abcdefght",
                        "000000000000000000.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000000.abcdefght",
                        "0000000006789.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("123")
                {
                    InputFileContent = new[]
                    {
                        "7878.a",
                        "9111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.a",
                        "9111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("124")
                {
                    InputFileContent = new[]
                    {
                        "9111.a",
                        "7878.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.a",
                        "9111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("125")
                {
                    InputFileContent = new[]
                    {
                        "7878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "9111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "9111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("126")
                {
                    InputFileContent = new[]
                    {
                        "9111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "7878.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "9111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("127")
                {
                    InputFileContent = new[]
                    {
                        "7878.abcdefght",
                        "9111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.abcdefght",
                        "9111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("128")
                {
                    InputFileContent = new[]
                    {
                        "9111.abcdefght",
                        "7878.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.abcdefght",
                        "9111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("129")
                {
                    InputFileContent = new[]
                    {
                        "7878.a",
                        "09111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.a",
                        "09111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("130")
                {
                    InputFileContent = new[]
                    {
                        "09111.a",
                        "7878.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.a",
                        "09111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("131")
                {
                    InputFileContent = new[]
                    {
                        "7878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "09111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "09111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("132")
                {
                    InputFileContent = new[]
                    {
                        "09111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "7878.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "09111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("133")
                {
                    InputFileContent = new[]
                    {
                        "7878.abcdefght",
                        "09111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.abcdefght",
                        "09111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("134")
                {
                    InputFileContent = new[]
                    {
                        "09111.abcdefght",
                        "7878.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.abcdefght",
                        "09111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("135")
                {
                    InputFileContent = new[]
                    {
                        "7878.a",
                        "00000000000000000009111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.a",
                        "00000000000000000009111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("136")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000009111.a",
                        "7878.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.a",
                        "00000000000000000009111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("137")
                {
                    InputFileContent = new[]
                    {
                        "9111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000009111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000009111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("138")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000009111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "7878.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000009111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("139")
                {
                    InputFileContent = new[]
                    {
                        "7878.abcdefght",
                        "00000000000000000009111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.abcdefght",
                        "00000000000000000009111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("140")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000009111.abcdefght",
                        "7878.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "7878.abcdefght",
                        "00000000000000000009111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("141")
                {
                    InputFileContent = new[]
                    {
                        "07878.a",
                        "9111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "07878.a",
                        "9111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("142")
                {
                    InputFileContent = new[]
                    {
                        "9111.a",
                        "07878.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "07878.a",
                        "9111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("143")
                {
                    InputFileContent = new[]
                    {
                        "07878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "9111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "07878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "9111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("144")
                {
                    InputFileContent = new[]
                    {
                        "9111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "07878.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "07878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "9111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("145")
                {
                    InputFileContent = new[]
                    {
                        "07878.abcdefght",
                        "9111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "07878.abcdefght",
                        "9111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("146")
                {
                    InputFileContent = new[]
                    {
                        "9111.abcdefght",
                        "07878.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "07878.abcdefght",
                        "9111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("147")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000007878.a",
                        "9111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000007878.a",
                        "9111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("148")
                {
                    InputFileContent = new[]
                    {
                        "9111.a",
                        "000000000000000007878.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000007878.a",
                        "9111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("149")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000007878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "9111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000007878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "9111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("150")
                {
                    InputFileContent = new[]
                    {
                        "9111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "000000000000000007878.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000007878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "9111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("151")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000007878.abcdefght",
                        "9111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000007878.abcdefght",
                        "9111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("152")
                {
                    InputFileContent = new[]
                    {
                        "9111.abcdefght",
                        "000000000000000007878.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000007878.abcdefght",
                        "9111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("153")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000007878.a",
                        "0000000009111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000007878.a",
                        "0000000009111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("154")
                {
                    InputFileContent = new[]
                    {
                        "0000000009111.a",
                        "000000000000000007878.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000007878.a",
                        "0000000009111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("155")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000007878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0000000009111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000007878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0000000009111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("156")
                {
                    InputFileContent = new[]
                    {
                        "0000000009111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "000000000000000007878.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000007878.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0000000009111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("157")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000007878.abcdefght",
                        "0000000009111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000007878.abcdefght",
                        "0000000009111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("158")
                {
                    InputFileContent = new[]
                    {
                        "0000000009111.abcdefght",
                        "000000000000000007878.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000007878.abcdefght",
                        "0000000009111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("159")
                {
                    InputFileContent = new[]
                    {
                        "111.a",
                        "1111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.a",
                        "1111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("160")
                {
                    InputFileContent = new[]
                    {
                        "1111.a",
                        "111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.a",
                        "1111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("161")
                {
                    InputFileContent = new[]
                    {
                        "111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("162")
                {
                    InputFileContent = new[]
                    {
                        "1111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("163")
                {
                    InputFileContent = new[]
                    {
                        "111.abcdefght",
                        "1111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.abcdefght",
                        "1111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("164")
                {
                    InputFileContent = new[]
                    {
                        "1111.abcdefght",
                        "111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.abcdefght",
                        "1111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("165")
                {
                    InputFileContent = new[]
                    {
                        "111.a",
                        "01111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.a",
                        "01111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("166")
                {
                    InputFileContent = new[]
                    {
                        "01111.a",
                        "111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.a",
                        "01111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("167")
                {
                    InputFileContent = new[]
                    {
                        "111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "01111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "01111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("168")
                {
                    InputFileContent = new[]
                    {
                        "01111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "01111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("169")
                {
                    InputFileContent = new[]
                    {
                        "111.abcdefght",
                        "01111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.abcdefght",
                        "01111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("170")
                {
                    InputFileContent = new[]
                    {
                        "01111.abcdefght",
                        "111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.abcdefght",
                        "01111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("171")
                {
                    InputFileContent = new[]
                    {
                        "111.a",
                        "00000000000000000001111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.a",
                        "00000000000000000001111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("172")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000001111.a",
                        "111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.a",
                        "00000000000000000001111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("173")
                {
                    InputFileContent = new[]
                    {
                        "111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000001111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000001111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("174")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000001111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000001111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("175")
                {
                    InputFileContent = new[]
                    {
                        "111.abcdefght",
                        "00000000000000000001111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.abcdefght",
                        "00000000000000000001111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("176")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000001111.abcdefght",
                        "111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "111.abcdefght",
                        "00000000000000000001111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("177")
                {
                    InputFileContent = new[]
                    {
                        "0111.a",
                        "1111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0111.a",
                        "1111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("178")
                {
                    InputFileContent = new[]
                    {
                        "1111.a",
                        "0111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0111.a",
                        "1111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("179")
                {
                    InputFileContent = new[]
                    {
                        "0111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("180")
                {
                    InputFileContent = new[]
                    {
                        "1111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("181")
                {
                    InputFileContent = new[]
                    {
                        "0111.abcdefght",
                        "1111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0111.abcdefght",
                        "1111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("182")
                {
                    InputFileContent = new[]
                    {
                        "1111.abcdefght",
                        "0111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0111.abcdefght",
                        "1111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("183")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000111.a",
                        "1111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00000000000000000111.a",
                        "1111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("184")
                {
                    InputFileContent = new[]
                    {
                        "1111.a",
                        "00000000000000000111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00000000000000000111.a",
                        "1111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("185")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00000000000000000111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("186")
                {
                    InputFileContent = new[]
                    {
                        "1111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00000000000000000111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "1111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("187")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000111.abcdefght",
                        "1111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00000000000000000111.abcdefght",
                        "1111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("188")
                {
                    InputFileContent = new[]
                    {
                        "1111.abcdefght",
                        "00000000000000000111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00000000000000000111.abcdefght",
                        "1111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("189")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000111.a",
                        "0000000001111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00000000000000000111.a",
                        "0000000001111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("190")
                {
                    InputFileContent = new[]
                    {
                        "0000000001111.a",
                        "00000000000000000111.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00000000000000000111.a",
                        "0000000001111.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("191")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0000000001111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00000000000000000111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0000000001111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("192")
                {
                    InputFileContent = new[]
                    {
                        "0000000001111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000000000000111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00000000000000000111.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "0000000001111.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("193")
                {
                    InputFileContent = new[]
                    {
                        "00000000000000000111.abcdefght",
                        "0000000001111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00000000000000000111.abcdefght",
                        "0000000001111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("194")
                {
                    InputFileContent = new[]
                    {
                        "0000000001111.abcdefght",
                        "00000000000000000111.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "00000000000000000111.abcdefght",
                        "0000000001111.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("195")
                {
                    InputFileContent = new[]
                    {
                        "2.a",
                        "11.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.a",
                        "11.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("196")
                {
                    InputFileContent = new[]
                    {
                        "11.a",
                        "2.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.a",
                        "11.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("197")
                {
                    InputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "11.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "11.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("198")
                {
                    InputFileContent = new[]
                    {
                        "11.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "11.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("199")
                {
                    InputFileContent = new[]
                    {
                        "2.abcdefght",
                        "11.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.abcdefght",
                        "11.abcdefght"
                    }
                };

                #endregion

                #region 200 - 299

                yield return new InputOutputTestCase.Seed("200")
                {
                    InputFileContent = new[]
                    {
                        "11.abcdefght",
                        "2.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.abcdefght",
                        "11.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("201")
                {
                    InputFileContent = new[]
                    {
                        "2.a",
                        "011.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.a",
                        "011.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("202")
                {
                    InputFileContent = new[]
                    {
                        "011.a",
                        "2.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.a",
                        "011.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("203")
                {
                    InputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "011.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "011.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("204")
                {
                    InputFileContent = new[]
                    {
                        "011.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "011.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("205")
                {
                    InputFileContent = new[]
                    {
                        "2.abcdefght",
                        "011.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.abcdefght",
                        "011.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("206")
                {
                    InputFileContent = new[]
                    {
                        "011.abcdefght",
                        "2.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.abcdefght",
                        "011.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("207")
                {
                    InputFileContent = new[]
                    {
                        "2.a",
                        "000000000000000000011.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.a",
                        "000000000000000000011.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("208")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000000011.a",
                        "2.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.a",
                        "000000000000000000011.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("209")
                {
                    InputFileContent = new[]
                    {
                        "11.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "000000000000000000011.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "11.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "000000000000000000011.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("210")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000000011.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "000000000000000000011.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("211")
                {
                    InputFileContent = new[]
                    {
                        "2.abcdefght",
                        "000000000000000000011.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.abcdefght",
                        "000000000000000000011.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("212")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000000011.abcdefght",
                        "2.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.abcdefght",
                        "000000000000000000011.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("213")
                {
                    InputFileContent = new[]
                    {
                        "02.a",
                        "11.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "02.a",
                        "11.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("214")
                {
                    InputFileContent = new[]
                    {
                        "11.a",
                        "02.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "02.a",
                        "11.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("215")
                {
                    InputFileContent = new[]
                    {
                        "02.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "11.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "02.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "11.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("216")
                {
                    InputFileContent = new[]
                    {
                        "11.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "02.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "02.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "11.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("217")
                {
                    InputFileContent = new[]
                    {
                        "02.abcdefght",
                        "11.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "02.abcdefght",
                        "11.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("218")
                {
                    InputFileContent = new[]
                    {
                        "11.abcdefght",
                        "02.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "02.abcdefght",
                        "11.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("219")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000002.a",
                        "11.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.a",
                        "11.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("220")
                {
                    InputFileContent = new[]
                    {
                        "11.a",
                        "000000000000000002.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.a",
                        "11.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("221")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "11.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "11.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("221")
                {
                    InputFileContent = new[]
                    {
                        "11.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "11.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("223")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000002.abcdefght",
                        "11.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.abcdefght",
                        "11.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("224")
                {
                    InputFileContent = new[]
                    {
                        "11.abcdefght",
                        "000000000000000002.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.abcdefght",
                        "11.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("225")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000002.a",
                        "00000000011.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.a",
                        "00000000011.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("226")
                {
                    InputFileContent = new[]
                    {
                        "00000000011.a",
                        "000000000000000002.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.a",
                        "00000000011.a"
                    }
                };

                yield return new InputOutputTestCase.Seed("227")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000011.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000011.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("228")
                {
                    InputFileContent = new[]
                    {
                        "00000000011.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.aaaaaaaaaaaaaaaaaaaaaaaaaa",
                        "00000000011.aaaaaaaaaaaaaaaaaaaaaaaaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("229")
                {
                    InputFileContent = new[]
                    {
                        "000000000000000002.abcdefght",
                        "00000000011.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.abcdefght",
                        "00000000011.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("230")
                {
                    InputFileContent = new[]
                    {
                        "00000000011.abcdefght",
                        "000000000000000002.abcdefght"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000000002.abcdefght",
                        "00000000011.abcdefght"
                    }
                };

                yield return new InputOutputTestCase.Seed("231")
                {
                    InputFileContent = new[]
                    {
                        "0.a",
                        "0.b",
                        "0.c",
                        "0.d",
                        "0.e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "0.b",
                        "0.c",
                        "0.d",
                        "0.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("232")
                {
                    InputFileContent = new[]
                    {
                        "0.b",
                        "0.a",
                        "0.c",
                        "0.d",
                        "0.e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "0.b",
                        "0.c",
                        "0.d",
                        "0.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("233")
                {
                    InputFileContent = new[]
                    {
                        "0.e",
                        "0.a",
                        "0.b",
                        "0.c",
                        "0.d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "0.b",
                        "0.c",
                        "0.d",
                        "0.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("234")
                {
                    InputFileContent = new[]
                    {
                        "0.e",
                        "0.b",
                        "0.c",
                        "0.d",
                        "0.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "0.b",
                        "0.c",
                        "0.d",
                        "0.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("235")
                {
                    InputFileContent = new[]
                    {
                        "0.a",
                        "0.b",
                        "0.d",
                        "0.e",
                        "0.c"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "0.b",
                        "0.c",
                        "0.d",
                        "0.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("236")
                {
                    InputFileContent = new[]
                    {
                        "0.d",
                        "0.a",
                        "0.e",
                        "0.c",
                        "0.b"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "0.b",
                        "0.c",
                        "0.d",
                        "0.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("237")
                {
                    InputFileContent = new[]
                    {
                        "0.d",
                        "0.a",
                        "0.b",
                        "0.e",
                        "0.c"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "0.b",
                        "0.c",
                        "0.d",
                        "0.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("238")
                {
                    InputFileContent = new[]
                    {
                        "0.a",
                        "000.b",
                        "00.c",
                        "000000.d",
                        "00.e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "000.b",
                        "00.c",
                        "000000.d",
                        "00.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("239")
                {
                    InputFileContent = new[]
                    {
                        "00.e",
                        "0.a",
                        "000.b",
                        "00.c",
                        "000000.d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "000.b",
                        "00.c",
                        "000000.d",
                        "00.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("241")
                {
                    InputFileContent = new[]
                    {
                        "000.b",
                        "00.c",
                        "000000.d",
                        "00.e",
                        "0.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "000.b",
                        "00.c",
                        "000000.d",
                        "00.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("242")
                {
                    InputFileContent = new[]
                    {
                        "00.e",
                        "000.b",
                        "00.c",
                        "000000.d",
                        "0.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "000.b",
                        "00.c",
                        "000000.d",
                        "00.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("243")
                {
                    InputFileContent = new[]
                    {
                        "00.e",
                        "000000.d",
                        "00.c",
                        "000.b",
                        "0.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "000.b",
                        "00.c",
                        "000000.d",
                        "00.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("244")
                {
                    InputFileContent = new[]
                    {
                        "000000.d",
                        "00.e",
                        "000.b",
                        "00.c",
                        "0.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.a",
                        "000.b",
                        "00.c",
                        "000000.d",
                        "00.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("245")
                {
                    InputFileContent = new[]
                    {
                        "1.a",
                        "1.b",
                        "1.c",
                        "1.d",
                        "1.e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.a",
                        "1.b",
                        "1.c",
                        "1.d",
                        "1.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("246")
                {
                    InputFileContent = new[]
                    {
                        "1.b",
                        "1.a",
                        "1.c",
                        "1.d",
                        "1.e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.a",
                        "1.b",
                        "1.c",
                        "1.d",
                        "1.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("247")
                {
                    InputFileContent = new[]
                    {
                        "1.e",
                        "1.a",
                        "1.b",
                        "1.c",
                        "1.d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.a",
                        "1.b",
                        "1.c",
                        "1.d",
                        "1.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("248")
                {
                    InputFileContent = new[]
                    {
                        "1.e",
                        "1.b",
                        "1.c",
                        "1.d",
                        "1.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.a",
                        "1.b",
                        "1.c",
                        "1.d",
                        "1.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("249")
                {
                    InputFileContent = new[]
                    {
                        "1.a",
                        "1.b",
                        "1.d",
                        "1.e",
                        "1.c"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.a",
                        "1.b",
                        "1.c",
                        "1.d",
                        "1.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("250")
                {
                    InputFileContent = new[]
                    {
                        "1.d",
                        "1.a",
                        "1.e",
                        "1.c",
                        "1.b"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.a",
                        "1.b",
                        "1.c",
                        "1.d",
                        "1.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("251")
                {
                    InputFileContent = new[]
                    {
                        "1.d",
                        "1.a",
                        "1.b",
                        "1.e",
                        "1.c"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.a",
                        "1.b",
                        "1.c",
                        "1.d",
                        "1.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("252")
                {
                    InputFileContent = new[]
                    {
                        "1.a",
                        "001.b",
                        "01.c",
                        "000001.d",
                        "01.e" 
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.a",
                        "001.b",
                        "01.c",
                        "000001.d",
                        "01.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("253")
                {
                    InputFileContent = new[]
                    {
                        "001.b",
                        "01.c",
                        "000001.d",
                        "01.e",
                        "1.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.a",
                        "001.b",
                        "01.c",
                        "000001.d",
                        "01.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("254")
                {
                    InputFileContent = new[]
                    {
                        "001.b",
                        "01.c",
                        "000001.d",
                        "01.e",
                        "1.a",
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.a",
                        "001.b",
                        "01.c",
                        "000001.d",
                        "01.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("255")
                {
                    InputFileContent = new[]
                    {
                        "000001.d",
                        "001.b",
                        "01.e",
                        "01.c",
                        "1.a",
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.a",
                        "001.b",
                        "01.c",
                        "000001.d",
                        "01.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("256")
                {
                    InputFileContent = new[]
                    {
                        "000001.d",
                        "1.a",
                        "001.b",
                        "01.c",
                        "01.e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.a",
                        "001.b",
                        "01.c",
                        "000001.d",
                        "01.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("257")
                {
                    InputFileContent = new[]
                    {
                        "3380.a",
                        "3380.b",
                        "3380.c",
                        "3380.d",
                        "3380.e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.a",
                        "3380.b",
                        "3380.c",
                        "3380.d",
                        "3380.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("258")
                {
                    InputFileContent = new[]
                    {
                        "3380.b",
                        "3380.a",
                        "3380.c",
                        "3380.d",
                        "3380.e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.a",
                        "3380.b",
                        "3380.c",
                        "3380.d",
                        "3380.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("259")
                {
                    InputFileContent = new[]
                    {
                        "3380.e",
                        "3380.a",
                        "3380.b",
                        "3380.c",
                        "3380.d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.a",
                        "3380.b",
                        "3380.c",
                        "3380.d",
                        "3380.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("260")
                {
                    InputFileContent = new[]
                    {
                        "3380.e",
                        "3380.b",
                        "3380.c",
                        "3380.d",
                        "3380.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.a",
                        "3380.b",
                        "3380.c",
                        "3380.d",
                        "3380.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("261")
                {
                    InputFileContent = new[]
                    {
                        "3380.a",
                        "3380.b",
                        "3380.d",
                        "3380.e",
                        "3380.c"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.a",
                        "3380.b",
                        "3380.c",
                        "3380.d",
                        "3380.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("262")
                {
                    InputFileContent = new[]
                    {
                        "3380.d",
                        "3380.a",
                        "3380.e",
                        "3380.c",
                        "3380.b"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.a",
                        "3380.b",
                        "3380.c",
                        "3380.d",
                        "3380.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("263")
                {
                    InputFileContent = new[]
                    {
                        "3380.d",
                        "3380.a",
                        "3380.b",
                        "3380.e",
                        "3380.c"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.a",
                        "3380.b",
                        "3380.c",
                        "3380.d",
                        "3380.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("264")
                {
                    InputFileContent = new[]
                    {
                        "3380.a",
                        "003380.b",
                        "03380.c",
                        "000003380.d",
                        "03380.e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.a",
                        "003380.b",
                        "03380.c",
                        "000003380.d",
                        "03380.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("265")
                {
                    InputFileContent = new[]
                    {
                        "003380.b",
                        "03380.c",
                        "000003380.d",
                        "03380.e",
                        "3380.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.a",
                        "003380.b",
                        "03380.c",
                        "000003380.d",
                        "03380.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("266")
                {
                    InputFileContent = new[]
                    {
                        "03380.e",
                        "3380.a",
                        "003380.b",
                        "03380.c",
                        "000003380.d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.a",
                        "003380.b",
                        "03380.c",
                        "000003380.d",
                        "03380.e"
                    }
                };
                
                yield return new InputOutputTestCase.Seed("267")
                {
                    InputFileContent = new[]
                    {
                        "03380.c",
                        "03380.e",
                        "003380.b",
                        "3380.a",
                        "000003380.d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.a",
                        "003380.b",
                        "03380.c",
                        "000003380.d",
                        "03380.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("268")
                {
                    InputFileContent = new[]
                    {
                        "03380.c",
                        "003380.b",
                        "3380.a",
                        "000003380.d",
                        "03380.e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.a",
                        "003380.b",
                        "03380.c",
                        "000003380.d",
                        "03380.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("269")
                {
                    InputFileContent = new[]
                    {
                        "9.a",
                        "8.b",
                        "7.c",
                        "6.d",
                        "5.e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.a",
                        "8.b",
                        "7.c",
                        "6.d",
                        "5.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("270")
                {
                    InputFileContent = new[]
                    {
                        "5.e",
                        "9.a",
                        "8.b",
                        "7.c",
                        "6.d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.a",
                        "8.b",
                        "7.c",
                        "6.d",
                        "5.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("271")
                {
                    InputFileContent = new[]
                    {
                        "8.b",
                        "7.c",
                        "6.d",
                        "5.e",
                        "9.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.a",
                        "8.b",
                        "7.c",
                        "6.d",
                        "5.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("272")
                {
                    InputFileContent = new[]
                    {
                        "5.e",
                        "8.b",
                        "7.c",
                        "6.d",
                        "9.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.a",
                        "8.b",
                        "7.c",
                        "6.d",
                        "5.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("273")
                {
                    InputFileContent = new[]
                    {
                        "5.e",
                        "7.c",
                        "6.d",
                        "8.b",
                        "9.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.a",
                        "8.b",
                        "7.c",
                        "6.d",
                        "5.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("274")
                {
                    InputFileContent = new[]
                    {
                        "9.a",
                        "008.b",
                        "07.c",
                        "000006.d",
                        "05.e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.a",
                        "008.b",
                        "07.c",
                        "000006.d",
                        "05.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("275")
                {
                    InputFileContent = new[]
                    {
                        "05.e",
                        "9.a",
                        "008.b",
                        "07.c",
                        "000006.d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.a",
                        "008.b",
                        "07.c",
                        "000006.d",
                        "05.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("276")
                {
                    InputFileContent = new[]
                    {
                        "008.b",
                        "07.c",
                        "000006.d",
                        "05.e",
                        "9.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.a",
                        "008.b",
                        "07.c",
                        "000006.d",
                        "05.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("277")
                {
                    InputFileContent = new[]
                    {
                        "05.e",
                        "008.b",
                        "07.c",
                        "000006.d",
                        "9.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.a",
                        "008.b",
                        "07.c",
                        "000006.d",
                        "05.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("278")
                {
                    InputFileContent = new[]
                    {
                        "008.b",
                        "000006.d",
                        "05.e",
                        "07.c",
                        "9.a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.a",
                        "008.b",
                        "07.c",
                        "000006.d",
                        "05.e"
                    }
                };

                yield return new InputOutputTestCase.Seed("279")
                {
                    InputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "0.sda gjsd skgdj yutwi 68765b",
                        "0.sda gjsd skgdj yutwi 68765c",
                        "0.sda gjsd skgdj yutwi 68765d",
                        "0.sda gjsd skgdj yutwi 68765e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "0.sda gjsd skgdj yutwi 68765b",
                        "0.sda gjsd skgdj yutwi 68765c",
                        "0.sda gjsd skgdj yutwi 68765d",
                        "0.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("280")
                {
                    InputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765b",
                        "0.sda gjsd skgdj yutwi 68765a",
                        "0.sda gjsd skgdj yutwi 68765c",
                        "0.sda gjsd skgdj yutwi 68765d",
                        "0.sda gjsd skgdj yutwi 68765e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "0.sda gjsd skgdj yutwi 68765b",
                        "0.sda gjsd skgdj yutwi 68765c",
                        "0.sda gjsd skgdj yutwi 68765d",
                        "0.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("281")
                {
                    InputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765e",
                        "0.sda gjsd skgdj yutwi 68765a",
                        "0.sda gjsd skgdj yutwi 68765b",
                        "0.sda gjsd skgdj yutwi 68765c",
                        "0.sda gjsd skgdj yutwi 68765d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "0.sda gjsd skgdj yutwi 68765b",
                        "0.sda gjsd skgdj yutwi 68765c",
                        "0.sda gjsd skgdj yutwi 68765d",
                        "0.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("282")
                {
                    InputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765e",
                        "0.sda gjsd skgdj yutwi 68765b",
                        "0.sda gjsd skgdj yutwi 68765c",
                        "0.sda gjsd skgdj yutwi 68765d",
                        "0.sda gjsd skgdj yutwi 68765a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "0.sda gjsd skgdj yutwi 68765b",
                        "0.sda gjsd skgdj yutwi 68765c",
                        "0.sda gjsd skgdj yutwi 68765d",
                        "0.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("283")
                {
                    InputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "0.sda gjsd skgdj yutwi 68765b",
                        "0.sda gjsd skgdj yutwi 68765d",
                        "0.sda gjsd skgdj yutwi 68765e",
                        "0.sda gjsd skgdj yutwi 68765c"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "0.sda gjsd skgdj yutwi 68765b",
                        "0.sda gjsd skgdj yutwi 68765c",
                        "0.sda gjsd skgdj yutwi 68765d",
                        "0.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("284")
                {
                    InputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765d",
                        "0.sda gjsd skgdj yutwi 68765a",
                        "0.sda gjsd skgdj yutwi 68765e",
                        "0.sda gjsd skgdj yutwi 68765c",
                        "0.sda gjsd skgdj yutwi 68765b"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "0.sda gjsd skgdj yutwi 68765b",
                        "0.sda gjsd skgdj yutwi 68765c",
                        "0.sda gjsd skgdj yutwi 68765d",
                        "0.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("285")
                {
                    InputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765d",
                        "0.sda gjsd skgdj yutwi 68765a",
                        "0.sda gjsd skgdj yutwi 68765b",
                        "0.sda gjsd skgdj yutwi 68765e",
                        "0.sda gjsd skgdj yutwi 68765c"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "0.sda gjsd skgdj yutwi 68765b",
                        "0.sda gjsd skgdj yutwi 68765c",
                        "0.sda gjsd skgdj yutwi 68765d",
                        "0.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("286")
                {
                    InputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "000.sda gjsd skgdj yutwi 68765b",
                        "00.sda gjsd skgdj yutwi 68765c",
                        "000000.sda gjsd skgdj yutwi 68765d",
                        "00.sda gjsd skgdj yutwi 68765e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "000.sda gjsd skgdj yutwi 68765b",
                        "00.sda gjsd skgdj yutwi 68765c",
                        "000000.sda gjsd skgdj yutwi 68765d",
                        "00.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("287")
                {
                    InputFileContent = new[]
                    {
                        "00.sda gjsd skgdj yutwi 68765e",
                        "0.sda gjsd skgdj yutwi 68765a",
                        "000.sda gjsd skgdj yutwi 68765b",
                        "00.sda gjsd skgdj yutwi 68765c",
                        "000000.sda gjsd skgdj yutwi 68765d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "000.sda gjsd skgdj yutwi 68765b",
                        "00.sda gjsd skgdj yutwi 68765c",
                        "000000.sda gjsd skgdj yutwi 68765d",
                        "00.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("288")
                {
                    InputFileContent = new[]
                    {
                        "000.sda gjsd skgdj yutwi 68765b",
                        "00.sda gjsd skgdj yutwi 68765c",
                        "000000.sda gjsd skgdj yutwi 68765d",
                        "00.sda gjsd skgdj yutwi 68765e",
                        "0.sda gjsd skgdj yutwi 68765a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "000.sda gjsd skgdj yutwi 68765b",
                        "00.sda gjsd skgdj yutwi 68765c",
                        "000000.sda gjsd skgdj yutwi 68765d",
                        "00.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("289")
                {
                    InputFileContent = new[]
                    {
                        "00.sda gjsd skgdj yutwi 68765e",
                        "000.sda gjsd skgdj yutwi 68765b",
                        "00.sda gjsd skgdj yutwi 68765c",
                        "000000.sda gjsd skgdj yutwi 68765d",
                        "0.sda gjsd skgdj yutwi 68765a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "000.sda gjsd skgdj yutwi 68765b",
                        "00.sda gjsd skgdj yutwi 68765c",
                        "000000.sda gjsd skgdj yutwi 68765d",
                        "00.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("290")
                {
                    InputFileContent = new[]
                    {
                        "00.sda gjsd skgdj yutwi 68765e",
                        "000000.sda gjsd skgdj yutwi 68765d",
                        "00.sda gjsd skgdj yutwi 68765c",
                        "000.sda gjsd skgdj yutwi 68765b",
                        "0.sda gjsd skgdj yutwi 68765a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "000.sda gjsd skgdj yutwi 68765b",
                        "00.sda gjsd skgdj yutwi 68765c",
                        "000000.sda gjsd skgdj yutwi 68765d",
                        "00.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("291")
                {
                    InputFileContent = new[]
                    {
                        "000000.sda gjsd skgdj yutwi 68765d",
                        "00.sda gjsd skgdj yutwi 68765e",
                        "000.sda gjsd skgdj yutwi 68765b",
                        "00.sda gjsd skgdj yutwi 68765c",
                        "0.sda gjsd skgdj yutwi 68765a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765a",
                        "000.sda gjsd skgdj yutwi 68765b",
                        "00.sda gjsd skgdj yutwi 68765c",
                        "000000.sda gjsd skgdj yutwi 68765d",
                        "00.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("292")
                {
                    InputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765a",
                        "1.sda gjsd skgdj yutwi 68765b",
                        "1.sda gjsd skgdj yutwi 68765c",
                        "1.sda gjsd skgdj yutwi 68765d",
                        "1.sda gjsd skgdj yutwi 68765e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765a",
                        "1.sda gjsd skgdj yutwi 68765b",
                        "1.sda gjsd skgdj yutwi 68765c",
                        "1.sda gjsd skgdj yutwi 68765d",
                        "1.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("293")
                {
                    InputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765b",
                        "1.sda gjsd skgdj yutwi 68765a",
                        "1.sda gjsd skgdj yutwi 68765c",
                        "1.sda gjsd skgdj yutwi 68765d",
                        "1.sda gjsd skgdj yutwi 68765e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765a",
                        "1.sda gjsd skgdj yutwi 68765b",
                        "1.sda gjsd skgdj yutwi 68765c",
                        "1.sda gjsd skgdj yutwi 68765d",
                        "1.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("294")
                {
                    InputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765e",
                        "1.sda gjsd skgdj yutwi 68765a",
                        "1.sda gjsd skgdj yutwi 68765b",
                        "1.sda gjsd skgdj yutwi 68765c",
                        "1.sda gjsd skgdj yutwi 68765d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765a",
                        "1.sda gjsd skgdj yutwi 68765b",
                        "1.sda gjsd skgdj yutwi 68765c",
                        "1.sda gjsd skgdj yutwi 68765d",
                        "1.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("295")
                {
                    InputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765e",
                        "1.sda gjsd skgdj yutwi 68765b",
                        "1.sda gjsd skgdj yutwi 68765c",
                        "1.sda gjsd skgdj yutwi 68765d",
                        "1.sda gjsd skgdj yutwi 68765a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765a",
                        "1.sda gjsd skgdj yutwi 68765b",
                        "1.sda gjsd skgdj yutwi 68765c",
                        "1.sda gjsd skgdj yutwi 68765d",
                        "1.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("296")
                {
                    InputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765a",
                        "1.sda gjsd skgdj yutwi 68765b",
                        "1.sda gjsd skgdj yutwi 68765d",
                        "1.sda gjsd skgdj yutwi 68765e",
                        "1.sda gjsd skgdj yutwi 68765c"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765a",
                        "1.sda gjsd skgdj yutwi 68765b",
                        "1.sda gjsd skgdj yutwi 68765c",
                        "1.sda gjsd skgdj yutwi 68765d",
                        "1.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("297")
                {
                    InputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765d",
                        "1.sda gjsd skgdj yutwi 68765a",
                        "1.sda gjsd skgdj yutwi 68765e",
                        "1.sda gjsd skgdj yutwi 68765c",
                        "1.sda gjsd skgdj yutwi 68765b"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765a",
                        "1.sda gjsd skgdj yutwi 68765b",
                        "1.sda gjsd skgdj yutwi 68765c",
                        "1.sda gjsd skgdj yutwi 68765d",
                        "1.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("298")
                {
                    InputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765d",
                        "1.sda gjsd skgdj yutwi 68765a",
                        "1.sda gjsd skgdj yutwi 68765b",
                        "1.sda gjsd skgdj yutwi 68765e",
                        "1.sda gjsd skgdj yutwi 68765c"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765a",
                        "1.sda gjsd skgdj yutwi 68765b",
                        "1.sda gjsd skgdj yutwi 68765c",
                        "1.sda gjsd skgdj yutwi 68765d",
                        "1.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("299")
                {
                    InputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765a",
                        "001.sda gjsd skgdj yutwi 68765b",
                        "01.sda gjsd skgdj yutwi 68765c",
                        "000001.sda gjsd skgdj yutwi 68765d",
                        "01.sda gjsd skgdj yutwi 68765e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765a",
                        "001.sda gjsd skgdj yutwi 68765b",
                        "01.sda gjsd skgdj yutwi 68765c",
                        "000001.sda gjsd skgdj yutwi 68765d",
                        "01.sda gjsd skgdj yutwi 68765e"
                    }
                };

                #endregion

                yield return new InputOutputTestCase.Seed("300")
                {
                    InputFileContent = new[]
                    {
                        "001.sda gjsd skgdj yutwi 68765b",
                        "01.sda gjsd skgdj yutwi 68765c",
                        "000001.sda gjsd skgdj yutwi 68765d",
                        "01.sda gjsd skgdj yutwi 68765e",
                        "1.sda gjsd skgdj yutwi 68765a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765a",
                        "001.sda gjsd skgdj yutwi 68765b",
                        "01.sda gjsd skgdj yutwi 68765c",
                        "000001.sda gjsd skgdj yutwi 68765d",
                        "01.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("301")
                {
                    InputFileContent = new[]
                    {
                        "001.sda gjsd skgdj yutwi 68765b",
                        "01.sda gjsd skgdj yutwi 68765c",
                        "000001.sda gjsd skgdj yutwi 68765d",
                        "01.sda gjsd skgdj yutwi 68765e",
                        "1.sda gjsd skgdj yutwi 68765a",
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765a",
                        "001.sda gjsd skgdj yutwi 68765b",
                        "01.sda gjsd skgdj yutwi 68765c",
                        "000001.sda gjsd skgdj yutwi 68765d",
                        "01.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("302")
                {
                    InputFileContent = new[]
                    {
                        "000001.sda gjsd skgdj yutwi 68765d",
                        "001.sda gjsd skgdj yutwi 68765b",
                        "01.sda gjsd skgdj yutwi 68765e",
                        "01.sda gjsd skgdj yutwi 68765c",
                        "1.sda gjsd skgdj yutwi 68765a",
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765a",
                        "001.sda gjsd skgdj yutwi 68765b",
                        "01.sda gjsd skgdj yutwi 68765c",
                        "000001.sda gjsd skgdj yutwi 68765d",
                        "01.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("303")
                {
                    InputFileContent = new[]
                    {
                        "000001.sda gjsd skgdj yutwi 68765d",
                        "1.sda gjsd skgdj yutwi 68765a",
                        "001.sda gjsd skgdj yutwi 68765b",
                        "01.sda gjsd skgdj yutwi 68765c",
                        "01.sda gjsd skgdj yutwi 68765e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.sda gjsd skgdj yutwi 68765a",
                        "001.sda gjsd skgdj yutwi 68765b",
                        "01.sda gjsd skgdj yutwi 68765c",
                        "000001.sda gjsd skgdj yutwi 68765d",
                        "01.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("304")
                {
                    InputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "3380.sda gjsd skgdj yutwi 68765b",
                        "3380.sda gjsd skgdj yutwi 68765c",
                        "3380.sda gjsd skgdj yutwi 68765d",
                        "3380.sda gjsd skgdj yutwi 68765e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "3380.sda gjsd skgdj yutwi 68765b",
                        "3380.sda gjsd skgdj yutwi 68765c",
                        "3380.sda gjsd skgdj yutwi 68765d",
                        "3380.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("305")
                {
                    InputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765b",
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "3380.sda gjsd skgdj yutwi 68765c",
                        "3380.sda gjsd skgdj yutwi 68765d",
                        "3380.sda gjsd skgdj yutwi 68765e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "3380.sda gjsd skgdj yutwi 68765b",
                        "3380.sda gjsd skgdj yutwi 68765c",
                        "3380.sda gjsd skgdj yutwi 68765d",
                        "3380.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("306")
                {
                    InputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765e",
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "3380.sda gjsd skgdj yutwi 68765b",
                        "3380.sda gjsd skgdj yutwi 68765c",
                        "3380.sda gjsd skgdj yutwi 68765d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "3380.sda gjsd skgdj yutwi 68765b",
                        "3380.sda gjsd skgdj yutwi 68765c",
                        "3380.sda gjsd skgdj yutwi 68765d",
                        "3380.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("307")
                {
                    InputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765e",
                        "3380.sda gjsd skgdj yutwi 68765b",
                        "3380.sda gjsd skgdj yutwi 68765c",
                        "3380.sda gjsd skgdj yutwi 68765d",
                        "3380.sda gjsd skgdj yutwi 68765a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "3380.sda gjsd skgdj yutwi 68765b",
                        "3380.sda gjsd skgdj yutwi 68765c",
                        "3380.sda gjsd skgdj yutwi 68765d",
                        "3380.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("308")
                {
                    InputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "3380.sda gjsd skgdj yutwi 68765b",
                        "3380.sda gjsd skgdj yutwi 68765d",
                        "3380.sda gjsd skgdj yutwi 68765e",
                        "3380.sda gjsd skgdj yutwi 68765c"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "3380.sda gjsd skgdj yutwi 68765b",
                        "3380.sda gjsd skgdj yutwi 68765c",
                        "3380.sda gjsd skgdj yutwi 68765d",
                        "3380.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("309")
                {
                    InputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765d",
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "3380.sda gjsd skgdj yutwi 68765e",
                        "3380.sda gjsd skgdj yutwi 68765c",
                        "3380.sda gjsd skgdj yutwi 68765b"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "3380.sda gjsd skgdj yutwi 68765b",
                        "3380.sda gjsd skgdj yutwi 68765c",
                        "3380.sda gjsd skgdj yutwi 68765d",
                        "3380.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("310")
                {
                    InputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765d",
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "3380.sda gjsd skgdj yutwi 68765b",
                        "3380.sda gjsd skgdj yutwi 68765e",
                        "3380.sda gjsd skgdj yutwi 68765c"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "3380.sda gjsd skgdj yutwi 68765b",
                        "3380.sda gjsd skgdj yutwi 68765c",
                        "3380.sda gjsd skgdj yutwi 68765d",
                        "3380.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("311")
                {
                    InputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "003380.sda gjsd skgdj yutwi 68765b",
                        "03380.sda gjsd skgdj yutwi 68765c",
                        "000003380.sda gjsd skgdj yutwi 68765d",
                        "03380.sda gjsd skgdj yutwi 68765e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "003380.sda gjsd skgdj yutwi 68765b",
                        "03380.sda gjsd skgdj yutwi 68765c",
                        "000003380.sda gjsd skgdj yutwi 68765d",
                        "03380.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("312")
                {
                    InputFileContent = new[]
                    {
                        "003380.sda gjsd skgdj yutwi 68765b",
                        "03380.sda gjsd skgdj yutwi 68765c",
                        "000003380.sda gjsd skgdj yutwi 68765d",
                        "03380.sda gjsd skgdj yutwi 68765e",
                        "3380.sda gjsd skgdj yutwi 68765a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "003380.sda gjsd skgdj yutwi 68765b",
                        "03380.sda gjsd skgdj yutwi 68765c",
                        "000003380.sda gjsd skgdj yutwi 68765d",
                        "03380.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("313")
                {
                    InputFileContent = new[]
                    {
                        "03380.sda gjsd skgdj yutwi 68765e",
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "003380.sda gjsd skgdj yutwi 68765b",
                        "03380.sda gjsd skgdj yutwi 68765c",
                        "000003380.sda gjsd skgdj yutwi 68765d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "003380.sda gjsd skgdj yutwi 68765b",
                        "03380.sda gjsd skgdj yutwi 68765c",
                        "000003380.sda gjsd skgdj yutwi 68765d",
                        "03380.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("314")
                {
                    InputFileContent = new[]
                    {
                        "03380.sda gjsd skgdj yutwi 68765c",
                        "03380.sda gjsd skgdj yutwi 68765e",
                        "003380.sda gjsd skgdj yutwi 68765b",
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "000003380.sda gjsd skgdj yutwi 68765d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "003380.sda gjsd skgdj yutwi 68765b",
                        "03380.sda gjsd skgdj yutwi 68765c",
                        "000003380.sda gjsd skgdj yutwi 68765d",
                        "03380.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("315")
                {
                    InputFileContent = new[]
                    {
                        "03380.sda gjsd skgdj yutwi 68765c",
                        "003380.sda gjsd skgdj yutwi 68765b",
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "000003380.sda gjsd skgdj yutwi 68765d",
                        "03380.sda gjsd skgdj yutwi 68765e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3380.sda gjsd skgdj yutwi 68765a",
                        "003380.sda gjsd skgdj yutwi 68765b",
                        "03380.sda gjsd skgdj yutwi 68765c",
                        "000003380.sda gjsd skgdj yutwi 68765d",
                        "03380.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("316")
                {
                    InputFileContent = new[]
                    {
                        "9.sda gjsd skgdj yutwi 68765a",
                        "8.sda gjsd skgdj yutwi 68765b",
                        "7.sda gjsd skgdj yutwi 68765c",
                        "6.sda gjsd skgdj yutwi 68765d",
                        "5.sda gjsd skgdj yutwi 68765e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.sda gjsd skgdj yutwi 68765a",
                        "8.sda gjsd skgdj yutwi 68765b",
                        "7.sda gjsd skgdj yutwi 68765c",
                        "6.sda gjsd skgdj yutwi 68765d",
                        "5.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("317")
                {
                    InputFileContent = new[]
                    {
                        "5.sda gjsd skgdj yutwi 68765e",
                        "9.sda gjsd skgdj yutwi 68765a",
                        "8.sda gjsd skgdj yutwi 68765b",
                        "7.sda gjsd skgdj yutwi 68765c",
                        "6.sda gjsd skgdj yutwi 68765d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.sda gjsd skgdj yutwi 68765a",
                        "8.sda gjsd skgdj yutwi 68765b",
                        "7.sda gjsd skgdj yutwi 68765c",
                        "6.sda gjsd skgdj yutwi 68765d",
                        "5.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("318")
                {
                    InputFileContent = new[]
                    {
                        "8.sda gjsd skgdj yutwi 68765b",
                        "7.sda gjsd skgdj yutwi 68765c",
                        "6.sda gjsd skgdj yutwi 68765d",
                        "5.sda gjsd skgdj yutwi 68765e",
                        "9.sda gjsd skgdj yutwi 68765a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.sda gjsd skgdj yutwi 68765a",
                        "8.sda gjsd skgdj yutwi 68765b",
                        "7.sda gjsd skgdj yutwi 68765c",
                        "6.sda gjsd skgdj yutwi 68765d",
                        "5.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("319")
                {
                    InputFileContent = new[]
                    {
                        "5.sda gjsd skgdj yutwi 68765e",
                        "8.sda gjsd skgdj yutwi 68765b",
                        "7.sda gjsd skgdj yutwi 68765c",
                        "6.sda gjsd skgdj yutwi 68765d",
                        "9.sda gjsd skgdj yutwi 68765a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.sda gjsd skgdj yutwi 68765a",
                        "8.sda gjsd skgdj yutwi 68765b",
                        "7.sda gjsd skgdj yutwi 68765c",
                        "6.sda gjsd skgdj yutwi 68765d",
                        "5.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("320")
                {
                    InputFileContent = new[]
                    {
                        "5.sda gjsd skgdj yutwi 68765e",
                        "7.sda gjsd skgdj yutwi 68765c",
                        "6.sda gjsd skgdj yutwi 68765d",
                        "8.sda gjsd skgdj yutwi 68765b",
                        "9.sda gjsd skgdj yutwi 68765a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.sda gjsd skgdj yutwi 68765a",
                        "8.sda gjsd skgdj yutwi 68765b",
                        "7.sda gjsd skgdj yutwi 68765c",
                        "6.sda gjsd skgdj yutwi 68765d",
                        "5.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("321")
                {
                    InputFileContent = new[]
                    {
                        "9.sda gjsd skgdj yutwi 68765a",
                        "008.sda gjsd skgdj yutwi 68765b",
                        "07.sda gjsd skgdj yutwi 68765c",
                        "000006.sda gjsd skgdj yutwi 68765d",
                        "05.sda gjsd skgdj yutwi 68765e"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.sda gjsd skgdj yutwi 68765a",
                        "008.sda gjsd skgdj yutwi 68765b",
                        "07.sda gjsd skgdj yutwi 68765c",
                        "000006.sda gjsd skgdj yutwi 68765d",
                        "05.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("322")
                {
                    InputFileContent = new[]
                    {
                        "05.sda gjsd skgdj yutwi 68765e",
                        "9.sda gjsd skgdj yutwi 68765a",
                        "008.sda gjsd skgdj yutwi 68765b",
                        "07.sda gjsd skgdj yutwi 68765c",
                        "000006.sda gjsd skgdj yutwi 68765d"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.sda gjsd skgdj yutwi 68765a",
                        "008.sda gjsd skgdj yutwi 68765b",
                        "07.sda gjsd skgdj yutwi 68765c",
                        "000006.sda gjsd skgdj yutwi 68765d",
                        "05.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("323")
                {
                    InputFileContent = new[]
                    {
                        "008.sda gjsd skgdj yutwi 68765b",
                        "07.sda gjsd skgdj yutwi 68765c",
                        "000006.sda gjsd skgdj yutwi 68765d",
                        "05.sda gjsd skgdj yutwi 68765e",
                        "9.sda gjsd skgdj yutwi 68765a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.sda gjsd skgdj yutwi 68765a",
                        "008.sda gjsd skgdj yutwi 68765b",
                        "07.sda gjsd skgdj yutwi 68765c",
                        "000006.sda gjsd skgdj yutwi 68765d",
                        "05.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("324")
                {
                    InputFileContent = new[]
                    {
                        "05.sda gjsd skgdj yutwi 68765e",
                        "008.sda gjsd skgdj yutwi 68765b",
                        "07.sda gjsd skgdj yutwi 68765c",
                        "000006.sda gjsd skgdj yutwi 68765d",
                        "9.sda gjsd skgdj yutwi 68765a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.sda gjsd skgdj yutwi 68765a",
                        "008.sda gjsd skgdj yutwi 68765b",
                        "07.sda gjsd skgdj yutwi 68765c",
                        "000006.sda gjsd skgdj yutwi 68765d",
                        "05.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("325")
                {
                    InputFileContent = new[]
                    {
                        "008.sda gjsd skgdj yutwi 68765b",
                        "000006.sda gjsd skgdj yutwi 68765d",
                        "05.sda gjsd skgdj yutwi 68765e",
                        "07.sda gjsd skgdj yutwi 68765c",
                        "9.sda gjsd skgdj yutwi 68765a"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "9.sda gjsd skgdj yutwi 68765a",
                        "008.sda gjsd skgdj yutwi 68765b",
                        "07.sda gjsd skgdj yutwi 68765c",
                        "000006.sda gjsd skgdj yutwi 68765d",
                        "05.sda gjsd skgdj yutwi 68765e"
                    }
                };

                yield return new InputOutputTestCase.Seed("326")
                {
                    InputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765",
                        "0.sda gjsd skgdj yutwi 68765",
                        "0.sda gjsd skgdj yutwi 68765",
                        "0.sda gjsd skgdj yutwi 68765",
                        "0.sda gjsd skgdj yutwi 68765"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.sda gjsd skgdj yutwi 68765",
                        "0.sda gjsd skgdj yutwi 68765",
                        "0.sda gjsd skgdj yutwi 68765",
                        "0.sda gjsd skgdj yutwi 68765",
                        "0.sda gjsd skgdj yutwi 68765"
                    }
                };

                yield return new InputOutputTestCase.Seed("327")
                {
                    InputFileContent = new[]
                    {
                        "0.",
                        "0.",
                        "0.",
                        "0.",
                        "0."
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.",
                        "0.",
                        "0.",
                        "0.",
                        "0."
                    }
                };

                yield return new InputOutputTestCase.Seed("328")
                {
                    InputFileContent = new[]
                    {
                        "1.",
                        "2.",
                        "3.",
                        "4.",
                        "5."
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.",
                        "2.",
                        "3.",
                        "4.",
                        "5."
                    }
                };

                yield return new InputOutputTestCase.Seed("329")
                {
                    InputFileContent = new[]
                    {
                        "5.",
                        "1.",
                        "4.",                  
                        "2.",
                        "3."
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.",
                        "2.",
                        "3.",
                        "4.",
                        "5."
                    }
                };

                yield return new InputOutputTestCase.Seed("330")
                {
                    InputFileContent = new[]
                    {
                        "5.b",
                        "1.a",
                        "4.",                  
                        "2.",
                        "3."
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "2.",
                        "3.",
                        "4.",
                        "1.a",
                        "5.b"
                    }
                };

                yield return new InputOutputTestCase.Seed("331")
                {
                    InputFileContent = new[]
                    {
                        "5.b",
                        "1.a",
                        "4.",
                        "2.a",
                        "3."
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3.",
                        "4.",
                        "1.a",
                        "2.a",
                        "5.b"
                    }
                };


                yield return new InputOutputTestCase.Seed("332")
                {
                    InputFileContent = new[]
                    {
                        "5.b",
                        "1.ac",
                        "4.",
                        "2.a",
                        "3."
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "3.",
                        "4.",
                        "2.a",
                        "1.ac",
                        "5.b"
                    }
                };

                yield return new InputOutputTestCase.Seed("333")
                {
                    InputFileContent = new[]
                    {
                        "000000000000.",
                        "0000000000000000000000.",
                        "000.",
                        "0.",
                        "00000000."
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000.",
                        "0000000000000000000000.",
                        "000.",
                        "0.",
                        "00000000."
                    }
                };

                yield return new InputOutputTestCase.Seed("334")
                {
                    InputFileContent = new[]
                    {
                        "000000000000.aaaa",
                        "0000000000000000000000.aaaa",
                        "000.aaaa",
                        "0.aaaa",
                        "00000000.aaaa"
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "000000000000.aaaa",
                        "0000000000000000000000.aaaa",
                        "000.aaaa",
                        "0.aaaa",
                        "00000000.aaaa"
                    }
                };

                yield return new InputOutputTestCase.Seed("335")
                {
                    InputFileContent = new[]
                    {
                        "1.",
                        "1.",
                        "4.",
                        "2.",
                        "2."
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.",
                        "1.",
                        "2.",
                        "2.",
                        "4."
                    }
                };

                yield return new InputOutputTestCase.Seed("336")
                {
                    InputFileContent = new[]
                    {
                        "1.",
                        "2.",
                        "4.",
                        "1.",
                        "2."
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "1.",
                        "1.",
                        "2.",
                        "2.",
                        "4."
                    }
                };

                yield return new InputOutputTestCase.Seed("337")
                {
                    InputFileContent = new[]
                    {
                        "1.",
                        "2.",
                        "4.",
                        "1.",
                        "2.",
                        "0."
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0.",
                        "1.",
                        "1.",
                        "2.",
                        "2.",
                        "4."
                    }
                };

                yield return new InputOutputTestCase.Seed("338")
                {
                    InputFileContent = new[]
                    {
                        "000000000001.",
                        "0000002.",
                        "0000000000000000000004.",
                        "000000001.",
                        "00000002.",
                        "0000."
                    },
                    ExpectedOutputFileContent = new[]
                    {
                        "0000.",
                        "000000000001.",
                        "000000001.",
                        "0000002.",
                        "00000002.",
                        "0000000000000000000004."
                    }
                };
            }
        }
    }
}

