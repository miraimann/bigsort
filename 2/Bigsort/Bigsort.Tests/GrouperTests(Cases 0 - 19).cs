using System.Collections.Generic;

namespace Bigsort.Tests
{
    public partial class GrouperTests
    {
        public static IEnumerable<TestCase> Cases1
        {
            get
            {
                var testCase = new TestCase("0")
                {
                    BufferSize = 1024,
                    Source = BytesOf(new[]
                    {
                        "111.ab~~~~~~~~~",
                        "111.aa~~~~~~~~~~~~~~~~~",
                        "111.aa~~~~~~",
                        "111.ab-------------",
                        "111.aa----------------------------",
                        "111.aa--------------------"
                    }, withEndLines: true),
                    ExpectedResult = new Dictionary<string, byte[]>
                    {
                        {
                            id("aa"), BytesOf(new[]
                            {
                                s(19, 3) + "111.aa~~~~~~~~~~~~~~~~~",
                                s(08, 3) + "111.aa~~~~~~",
                                s(30, 3) + "111.aa----------------------------",
                                s(22, 3) + "111.aa--------------------"
                            }, withEndLines: false)
                        },
                        {
                            id("ab"), BytesOf(new[]
                            {
                                s(11, 3) + "111.ab~~~~~~~~~",
                                s(15, 3) + "111.ab-------------"
                            }, withEndLines: false)
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
                    Source = BytesOf(new[]
                    {
                        "1.",
                        "1.",
                        "1.",
                        "1.",
                        "1.",
                        "1.",
                        "1."
                    }, withEndLines: true),
                    ExpectedResult = new Dictionary<string, byte[]>
                    {
                        {
                            id(""), BytesOf(new[]
                            {
                                s(0, 1) + "1.",
                                s(0, 1) + "1.",
                                s(0, 1) + "1.",
                                s(0, 1) + "1.",
                                s(0, 1) + "1.",
                                s(0, 1) + "1.",
                                s(0, 1) + "1."
                            }, withEndLines: false)
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
                    Source = BytesOf(new[]
                    {
                        "111111.a",
                        "111111.a",
                        "111111.a",
                        "111111.a",
                        "111111.a",
                        "111111.a",
                        "111111.a"
                    }, withEndLines: true),
                    ExpectedResult = new Dictionary<string, byte[]>
                    {
                        {
                            id("a"), BytesOf(new[]
                            {
                                s(1, 6) + "111111.a",
                                s(1, 6) + "111111.a",
                                s(1, 6) + "111111.a",
                                s(1, 6) + "111111.a",
                                s(1, 6) + "111111.a",
                                s(1, 6) + "111111.a",
                                s(1, 6) + "111111.a"
                            }, withEndLines: false)
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
                    Source = BytesOf(new[]
                    {
                        "1.a",
                        "1.b",
                        "1.c",
                        "1.d",
                        "1.e",
                        "1.f",
                        "1.g"
                    }, withEndLines: true),
                    ExpectedResult = new Dictionary<string, byte[]>
                    {
                        { id("a"), BytesOf(new[] { s(1, 1) + "1.a" }, false) },
                        { id("b"), BytesOf(new[] { s(1, 1) + "1.b" }, false) },
                        { id("c"), BytesOf(new[] { s(1, 1) + "1.c" }, false) },
                        { id("d"), BytesOf(new[] { s(1, 1) + "1.d" }, false) },
                        { id("e"), BytesOf(new[] { s(1, 1) + "1.e" }, false) },
                        { id("f"), BytesOf(new[] { s(1, 1) + "1.f" }, false) },
                        { id("g"), BytesOf(new[] { s(1, 1) + "1.g" }, false) }
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
                    Source = BytesOf(new[]
                    {
                        "1.a'''''''''''''''''",
                        "1.b'''''''''''''''''",
                        "1.c'''''''''''''''''",
                        "1.d'''''''''''''''''",
                        "1.e'''''''''''''''''",
                        "1.f'''''''''''''''''",
                        "1.g'''''''''''''''''"
                    }, withEndLines: true),
                    ExpectedResult = new Dictionary<string, byte[]>
                    {
                        { id("a'"), BytesOf(new[] { s(18, 1) + "1.a'''''''''''''''''" }, false) },
                        { id("b'"), BytesOf(new[] { s(18, 1) + "1.b'''''''''''''''''" }, false) },
                        { id("c'"), BytesOf(new[] { s(18, 1) + "1.c'''''''''''''''''" }, false) },
                        { id("d'"), BytesOf(new[] { s(18, 1) + "1.d'''''''''''''''''" }, false) },
                        { id("e'"), BytesOf(new[] { s(18, 1) + "1.e'''''''''''''''''" }, false) },
                        { id("f'"), BytesOf(new[] { s(18, 1) + "1.f'''''''''''''''''" }, false) },
                        { id("g'"), BytesOf(new[] { s(18, 1) + "1.g'''''''''''''''''" }, false) }
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
                    Source = BytesOf(new[]
                    {
                        "123.a",
                        "11.bb",
                        "11111111.",
                        "132323.d",
                        "133.",
                        "1.bbccc",
                        "12.g"
                    }, withEndLines: true),
                    ExpectedResult = new Dictionary<string, byte[]>
                    {
                        {
                            id("a"), BytesOf(new[]
                                {
                                    s(1, 3) + "123.a"
                                }, withEndLines: false)
                        },
                        {
                            id("bb"), BytesOf(new[]
                                {
                                    s(2, 2) + "11.bb",
                                    s(5, 1) + "1.bbccc"
                                }, withEndLines: false)
                        },
                        {
                            id(""), BytesOf(new[]
                                {
                                    s(0, 8) + "11111111.",
                                    s(0, 3) + "133."
                                }, withEndLines: false)
                        },
                        {
                            id("d"), BytesOf(new[]
                                {
                                    s(1, 6) + "132323.d"
                                }, withEndLines: false)
                        },
                        {
                            id("g"), BytesOf(new[]
                                {
                                    s(1, 2) + "12.g"
                                }, withEndLines: false)
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
