using System.Collections.Generic;
using static Bigsort.Tests.Tools;

namespace Bigsort.Tests
{
    public partial class GrouperTests
    {
        // ReSharper disable once InconsistentNaming
        public static IEnumerable<TestCase> Cases_00_19
        {
            get
            {
                var testCase = new TestCase("0")
                {
                    BufferSize = 1024,
                    Source = BytesOfString_s(new[]
                    {
                        "111.ab~~~~~~~~~",
                        "111.aa~~~~~~~~~~~~~~~~~",
                        "111.aa~~~~~~",
                        "111.ab-------------",
                        "111.aa----------------------------",
                        "111.aa--------------------"
                    }, addEndLines: true),
                    ExpectedResult = new Dictionary<string, byte[]>
                    {
                        {
                            id("aa"), BytesOfString_s(new[]
                            {
                                s(19, 3) + "111.aa~~~~~~~~~~~~~~~~~",
                                s(08, 3) + "111.aa~~~~~~",
                                s(30, 3) + "111.aa----------------------------",
                                s(22, 3) + "111.aa--------------------"
                            }, addEndLines: false)
                        },
                        {
                            id("ab"), BytesOfString_s(new[]
                            {
                                s(11, 3) + "111.ab~~~~~~~~~",
                                s(15, 3) + "111.ab-------------"
                            }, addEndLines: false)
                        }
                    }
                };

                yield return testCase;

                yield return new TestCase("1")
                {
                    BufferSize = 75,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };

                yield return new TestCase("2")
                {
                    BufferSize = 42,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };

                testCase = new TestCase("3")
                {
                    BufferSize = 6,
                    Source = BytesOfString_s(new[]
                    {
                        "1.",
                        "1.",
                        "1.",
                        "1.",
                        "1.",
                        "1.",
                        "1."
                    }, addEndLines: true),
                    ExpectedResult = new Dictionary<string, byte[]>
                    {
                        {
                            id(""), BytesOfString_s(new[]
                            {
                                s(0, 1) + "1.",
                                s(0, 1) + "1.",
                                s(0, 1) + "1.",
                                s(0, 1) + "1.",
                                s(0, 1) + "1.",
                                s(0, 1) + "1.",
                                s(0, 1) + "1."
                            }, addEndLines: false)
                        }
                    }
                };

                yield return testCase;

                yield return new TestCase("4")
                {
                    BufferSize = 9,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };

                yield return new TestCase("5")
                {
                    BufferSize = 15,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };

                testCase = new TestCase("6")
                {
                    BufferSize = 12,
                    Source = BytesOfString_s(new[]
                    {
                        "111111.a",
                        "111111.a",
                        "111111.a",
                        "111111.a",
                        "111111.a",
                        "111111.a",
                        "111111.a"
                    }, addEndLines: true),
                    ExpectedResult = new Dictionary<string, byte[]>
                    {
                        {
                            id("a"), BytesOfString_s(new[]
                            {
                                s(1, 6) + "111111.a",
                                s(1, 6) + "111111.a",
                                s(1, 6) + "111111.a",
                                s(1, 6) + "111111.a",
                                s(1, 6) + "111111.a",
                                s(1, 6) + "111111.a",
                                s(1, 6) + "111111.a"
                            }, addEndLines: false)
                        }
                    }
                };

                yield return testCase;

                yield return new TestCase("7")
                {
                    BufferSize = 15,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };

                yield return new TestCase("8")
                {
                    BufferSize = 36,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };

                testCase = new TestCase("9")
                {
                    BufferSize = 9,
                    Source = BytesOfString_s(new[]
                    {
                        "1.a",
                        "1.b",
                        "1.c",
                        "1.d",
                        "1.e",
                        "1.f",
                        "1.g"
                    }, addEndLines: true),
                    ExpectedResult = new Dictionary<string, byte[]>
                    {
                        { id("a"), BytesOfString_s(new[] { s(1, 1) + "1.a" }, false) },
                        { id("b"), BytesOfString_s(new[] { s(1, 1) + "1.b" }, false) },
                        { id("c"), BytesOfString_s(new[] { s(1, 1) + "1.c" }, false) },
                        { id("d"), BytesOfString_s(new[] { s(1, 1) + "1.d" }, false) },
                        { id("e"), BytesOfString_s(new[] { s(1, 1) + "1.e" }, false) },
                        { id("f"), BytesOfString_s(new[] { s(1, 1) + "1.f" }, false) },
                        { id("g"), BytesOfString_s(new[] { s(1, 1) + "1.g" }, false) }
                    }
                };

                yield return testCase;

                yield return new TestCase("10")
                {
                    BufferSize = 12,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };

                yield return new TestCase("11")
                {
                    BufferSize = 21,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };

                testCase = new TestCase("12")
                {
                    BufferSize = 24,
                    Source = BytesOfString_s(new[]
                    {
                        "1.a'''''''''''''''''",
                        "1.b'''''''''''''''''",
                        "1.c'''''''''''''''''",
                        "1.d'''''''''''''''''",
                        "1.e'''''''''''''''''",
                        "1.f'''''''''''''''''",
                        "1.g'''''''''''''''''"
                    }, addEndLines: true),
                    ExpectedResult = new Dictionary<string, byte[]>
                    {
                        { id("a'"), BytesOfString_s(new[] { s(18, 1) + "1.a'''''''''''''''''" }, false) },
                        { id("b'"), BytesOfString_s(new[] { s(18, 1) + "1.b'''''''''''''''''" }, false) },
                        { id("c'"), BytesOfString_s(new[] { s(18, 1) + "1.c'''''''''''''''''" }, false) },
                        { id("d'"), BytesOfString_s(new[] { s(18, 1) + "1.d'''''''''''''''''" }, false) },
                        { id("e'"), BytesOfString_s(new[] { s(18, 1) + "1.e'''''''''''''''''" }, false) },
                        { id("f'"), BytesOfString_s(new[] { s(18, 1) + "1.f'''''''''''''''''" }, false) },
                        { id("g'"), BytesOfString_s(new[] { s(18, 1) + "1.g'''''''''''''''''" }, false) }
                    }
                };

                yield return testCase;

                yield return new TestCase("13")
                {
                    BufferSize = 27,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };

                yield return new TestCase("14")
                {
                    BufferSize = 30,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };

                yield return new TestCase("15")
                {
                    BufferSize = 33,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };

                yield return new TestCase("16")
                {
                    BufferSize = 63,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };

                testCase = new TestCase("17")
                {
                    BufferSize = 12,
                    Source = BytesOfString_s(new[]
                    {
                        "123.a",
                        "11.bb",
                        "11111111.",
                        "132323.d",
                        "133.",
                        "1.bbccc",
                        "12.g"
                    }, addEndLines: true),
                    ExpectedResult = new Dictionary<string, byte[]>
                    {
                        {
                            id("a"), BytesOfString_s(new[]
                                {
                                    s(1, 3) + "123.a"
                                }, addEndLines: false)
                        },
                        {
                            id("bb"), BytesOfString_s(new[]
                                {
                                    s(2, 2) + "11.bb",
                                    s(5, 1) + "1.bbccc"
                                }, addEndLines: false)
                        },
                        {
                            id(""), BytesOfString_s(new[]
                                {
                                    s(0, 8) + "11111111.",
                                    s(0, 3) + "133."
                                }, addEndLines: false)
                        },
                        {
                            id("d"), BytesOfString_s(new[]
                                {
                                    s(1, 6) + "132323.d"
                                }, addEndLines: false)
                        },
                        {
                            id("g"), BytesOfString_s(new[]
                                {
                                    s(1, 2) + "12.g"
                                }, addEndLines: false)
                        }
                    }
                };

                yield return testCase;

                yield return new TestCase("18")
                {
                    BufferSize = 15,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };

                yield return new TestCase("19")
                {
                    BufferSize = 33,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };
            }
        }
    }
}
