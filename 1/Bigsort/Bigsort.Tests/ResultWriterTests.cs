using Bigsort.Contracts;
using Bigsort.Implementation;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Moq;

// ReSharper disable InconsistentNaming

namespace Bigsort.Tests
{
    [TestFixture]
    public class ResultWriterTests
    {
        private IResultWriter _writer;

        private MemoryStream _inputStream, _outputStream;
        private IoService _ioService;
        private Mock<IConfig> _config;

        [SetUp]
        public void SetUp()
        {
            _inputStream = new MemoryStream();
            _outputStream = new MemoryStream();

            _config = new Mock<IConfig>();
            _config.SetupGet(o => o.EndLine)
                   .Returns(Consts.EndLineBytes);
            _config.SetupGet(o => o.BytesEnumeratingBufferSize)
                   .Returns(1024);

            _ioService = new IoService(_config.Object);
            _writer = new ResultWriter(_config.Object);
        }

        [TestCaseSource(nameof(Data))]
        public void Test(int buffSize, MappedTestCase.Seed seed)
        {
            var testCase = MappedTestCase.Parse(seed);
            _inputStream.Write(testCase.FilesSource.InputFileContent,
                0, testCase.FilesSource.InputFileContent.Length);
            _inputStream.Position = 0;

            _config.SetupGet(o => o.ResultWriterBufferSize)
                   .Returns(buffSize);
            
            var output = _ioService.Adapt(_outputStream);
            var input = new IndexedInput
            {
                Bytes = _ioService.Adapt(_inputStream),
                LinesStarts = new ReadOnlyCollection<long>(testCase.LinesStarts),
                LinesEnds = testCase.LinesStarts
                                    .Skip(1)
                                    .Concat(new[] { _inputStream.Length })
                                    .Select(i => i - 1)
                                    .ToList()
            };

            _writer.Write(input, output, testCase.LinesOrdering);

            CollectionAssert.AreEqual(
                testCase.FilesSource.ExpectedOutputFileContent,
                _outputStream.ToArray());
        }

        public static IEnumerable<TestCaseData> Data =>
            Enumerable.Join(
                new[] { 2, 3, 4, 7, 17, 42, 123, 1024, 32 * 1024 },
                Seeds, 
                _ => true,
                _ => true,
                (buffSize, seed) => new TestCaseData(buffSize, seed));
        
        public static IEnumerable<MappedTestCase.Seed> Seeds
        {
            get
            {
                var end = Consts.EndLine;
                const string
                    _______WWWwwwwWWWWWWWWWWWw_______ = "WWWwwwwWWWWWWWWWWWw",
                    _______VVVvvvvVVVvVVVVV__________ = "VVVvvvvVVVvVVVVV",
                    _______ZZZzzzzZZZZZZ_____________ = "ZZZzzzzZZZZZZ",
                    _______OOOoooooOOOOOooOooooOO____ = "OOOoooooOOOOOooOooooOO";

                yield return new MappedTestCase.Seed("0")
                {
                    FilesSource = new InputOutputTestCase.Seed
                    {
                        InputFileContent = new string[0],
                        ExpectedOutputFileContent = new string[0],
                    },
                    LinesOrdering = new int[0],
                    LineStars = new long[0]
                };

                yield return new MappedTestCase.Seed("1")
                {
                    FilesSource = new InputOutputTestCase.Seed
                    {
                        InputFileContent = new[]
                        {
                            _______WWWwwwwWWWWWWWWWWWw_______
                        },
                        ExpectedOutputFileContent = new[]
                        {
                            _______WWWwwwwWWWWWWWWWWWw_______,
                        },
                    },
                    LinesOrdering = new[] { 0 },
                    LineStars = new[] { 0L }
                };

                yield return new MappedTestCase.Seed("2")
                {
                    FilesSource = new InputOutputTestCase.Seed
                    {
                        InputFileContent = new[]
                        {
                            _______WWWwwwwWWWWWWWWWWWw_______ + end
                        },
                        ExpectedOutputFileContent = new[]
                        {
                            _______WWWwwwwWWWWWWWWWWWw_______
                        }
                    },
                    LinesOrdering = new[] { 0 },
                    LineStars = new[] { 0L }
                };

                yield return new MappedTestCase.Seed("3")
                {
                    FilesSource = new InputOutputTestCase.Seed
                    {
                        InputFileContent = new[]
                        {
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______VVVvvvvVVVvVVVVV__________
                        },
                        ExpectedOutputFileContent = new[]
                        {
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______VVVvvvvVVVvVVVVV__________
                        },
                    },
                    LinesOrdering = new[] { 0, 1 },
                    LineStars = new[]
                    {
                        /* 0 */ 0L,
                        /* 1 */ _______WWWwwwwWWWWWWWWWWWw_______.Length + end.Length
                    }
                };

                 yield return new MappedTestCase.Seed("4")
                 {
                    FilesSource = new InputOutputTestCase.Seed
                    {
                        InputFileContent = new[]
                        {
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______VVVvvvvVVVvVVVVV__________
                        },
                        ExpectedOutputFileContent = new[]
                        {
                            _______VVVvvvvVVVvVVVVV__________,
                            _______WWWwwwwWWWWWWWWWWWw_______
                        }
                    },
                    LinesOrdering = new[] { 1, 0 },
                    LineStars = new[]
                    {
                        /* 0 */ 0L,
                        /* 1 */ _______WWWwwwwWWWWWWWWWWWw_______.Length + end.Length
                    },
                };

                var lineStarts = new[]
                    {
                        /* 0 */ 0L,
                        /* 1 */ _______WWWwwwwWWWWWWWWWWWw_______.Length + end.Length,
                        /* 2 */ _______WWWwwwwWWWWWWWWWWWw_______.Length + end.Length +
                                _______VVVvvvvVVVvVVVVV__________.Length + end.Length,
                        /* 3 */ _______WWWwwwwWWWWWWWWWWWw_______.Length + end.Length +
                                _______VVVvvvvVVVvVVVVV__________.Length + end.Length +
                                _______ZZZzzzzZZZZZZ_____________.Length + end.Length,
                    };

                yield return new MappedTestCase.Seed("5")
                {
                    FilesSource = new InputOutputTestCase.Seed
                    {
                        InputFileContent = new[]
                        {
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______VVVvvvvVVVvVVVVV__________,
                            _______ZZZzzzzZZZZZZ_____________,
                            _______OOOoooooOOOOOooOooooOO____
                        },
                        ExpectedOutputFileContent = new[]
                        {
                            _______ZZZzzzzZZZZZZ_____________,
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______VVVvvvvVVVvVVVVV__________,
                            _______OOOoooooOOOOOooOooooOO____
                        }
                    },
                    LinesOrdering = new[] { 2, 0, 1, 3 },
                    LineStars = lineStarts
                };

                yield return new MappedTestCase.Seed("6")
                {
                    FilesSource = new InputOutputTestCase.Seed
                    {
                        InputFileContent = new[]
                        {
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______VVVvvvvVVVvVVVVV__________,
                            _______ZZZzzzzZZZZZZ_____________,
                            _______OOOoooooOOOOOooOooooOO____
                        },
                        ExpectedOutputFileContent = new[]
                        {
                            _______VVVvvvvVVVvVVVVV__________,
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______OOOoooooOOOOOooOooooOO____,
                            _______ZZZzzzzZZZZZZ_____________
                        }
                    },
                    LinesOrdering = new[] { 1, 0, 3, 2 },
                    LineStars = lineStarts
                };

                yield return new MappedTestCase.Seed("7")
                {
                    FilesSource = new InputOutputTestCase.Seed
                    {
                        InputFileContent = new[]
                        {
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______VVVvvvvVVVvVVVVV__________,
                            _______ZZZzzzzZZZZZZ_____________,
                            _______OOOoooooOOOOOooOooooOO____
                        },
                        ExpectedOutputFileContent = new[]
                        {
                            _______OOOoooooOOOOOooOooooOO____,
                            _______VVVvvvvVVVvVVVVV__________,
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______ZZZzzzzZZZZZZ_____________
                        }
                    },
                    LinesOrdering = new[] { 3, 1, 0, 2 },
                    LineStars = lineStarts
                };

                yield return new MappedTestCase.Seed("8")
                {
                    FilesSource = new InputOutputTestCase.Seed
                    {
                        InputFileContent = new[]
                        {
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______VVVvvvvVVVvVVVVV__________ + end
                        },
                        ExpectedOutputFileContent = new[]
                        {
                            _______VVVvvvvVVVvVVVVV__________,
                            _______WWWwwwwWWWWWWWWWWWw_______
                        }
                    },
                    LinesOrdering = new[] { 1, 0 },
                    LineStars = new[]
                    {
                        /* 0 */ 0L,
                        /* 1 */ _______WWWwwwwWWWWWWWWWWWw_______.Length + end.Length
                    },
                };

                yield return new MappedTestCase.Seed("9")
                {
                    FilesSource = new InputOutputTestCase.Seed
                    {
                        InputFileContent = new[]
                        {
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______VVVvvvvVVVvVVVVV__________,
                            _______ZZZzzzzZZZZZZ_____________,
                            _______OOOoooooOOOOOooOooooOO____ + end
                        },
                        ExpectedOutputFileContent = new[]
                        {
                            _______ZZZzzzzZZZZZZ_____________,
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______VVVvvvvVVVvVVVVV__________,
                            _______OOOoooooOOOOOooOooooOO____
                        }
                    },
                    LinesOrdering = new[] { 2, 0, 1, 3 },
                    LineStars = lineStarts
                };

                yield return new MappedTestCase.Seed("10")
                {
                    FilesSource = new InputOutputTestCase.Seed
                    {
                        InputFileContent = new[]
                        {
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______VVVvvvvVVVvVVVVV__________,
                            _______ZZZzzzzZZZZZZ_____________,
                            _______OOOoooooOOOOOooOooooOO____ + end
                        },
                        ExpectedOutputFileContent = new[]
                        {
                            _______VVVvvvvVVVvVVVVV__________,
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______OOOoooooOOOOOooOooooOO____,
                            _______ZZZzzzzZZZZZZ_____________
                        }
                    },
                    LinesOrdering = new[] { 1, 0, 3, 2 },
                    LineStars = lineStarts
                };

                yield return new MappedTestCase.Seed("11")
                {
                    FilesSource = new InputOutputTestCase.Seed
                    {
                        InputFileContent = new[]
                        {
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______VVVvvvvVVVvVVVVV__________,
                            _______ZZZzzzzZZZZZZ_____________,
                            _______OOOoooooOOOOOooOooooOO____ + end
                        },
                        ExpectedOutputFileContent = new[]
                        {
                            _______OOOoooooOOOOOooOooooOO____,
                            _______VVVvvvvVVVvVVVVV__________,
                            _______WWWwwwwWWWWWWWWWWWw_______,
                            _______ZZZzzzzZZZZZZ_____________
                        },
                    },
                    LinesOrdering = new[] { 3, 1, 0, 2 },
                    LineStars = lineStarts
                };
            }
        }
    }
}
