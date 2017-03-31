//using System.Collections.Generic;

//namespace Bigsort.Tests
//{
//    // ReSharper disable once InconsistentNaming
//    public partial class Grouper_255_255x255Tests
//    {
//        public static IEnumerable<TestCase> Cases1
//        {
//            get
//            {
//                var testCase = new TestCase("0")
//                {
//                    BufferSize = 1024,
//                    Source = BytesOf(new[]
//                    {
//                        "111.ab~~~~~~~~~",
//                        "111.aa~~~~~~~~~~~~~~~~~",
//                        "111.aa~~~~~~",
//                        "111.ab-------------",
//                        "111.aa----------------------------",
//                        "111.aa--------------------"
//                    }, withEndLines: true),
//                    ExpectedResult = new Dictionary<string, byte[]>
//                    {
//                        {
//                            id("aa"), BytesOf(new[]
//                            {
//                                s(0, 3) + "111" + s(19) + "aa~~~~~~~~~~~~~~~~~",
//                                s(0, 3) + "111" + s(08) + "aa~~~~~~",
//                                s(0, 3) + "111" + s(30) + "aa----------------------------",
//                                s(0, 3) + "111" + s(22) + "aa--------------------"
//                            }, withEndLines: false)
//                        },
//                        {
//                            id("ab"), BytesOf(new[]
//                            {
//                                s(0, 3) + "111" + s(11) + "ab~~~~~~~~~",
//                                s(0, 3) + "111" + s(15) + "ab-------------"
//                            }, withEndLines: false)
//                        }
//                    }
//                };

//                yield return testCase;

//                yield return new TestCase("1")
//                {
//                    BufferSize = 25,
//                    Source = testCase.Source,
//                    ExpectedResult = testCase.ExpectedResult
//                };

//                yield return new TestCase("2")
//                {
//                    BufferSize = 14,
//                    Source = testCase.Source,
//                    ExpectedResult = testCase.ExpectedResult
//                };

//                testCase = new TestCase("3")
//                {
//                    BufferSize = 2,
//                    Source = BytesOf(new[]
//                    {
//                        "1.",
//                        "1.",
//                        "1.",
//                        "1.",
//                        "1.",
//                        "1.",
//                        "1."
//                    }, withEndLines: true),
//                    ExpectedResult = new Dictionary<string, byte[]>
//                    {
//                        {
//                            id(""), BytesOf(new[]
//                            {
//                                s(0, 1) + "1" + s(0),
//                                s(0, 1) + "1" + s(0),
//                                s(0, 1) + "1" + s(0),
//                                s(0, 1) + "1" + s(0),
//                                s(0, 1) + "1" + s(0),
//                                s(0, 1) + "1" + s(0),
//                                s(0, 1) + "1" + s(0)
//                            }, withEndLines: false)
//                        }
//                    }
//                };

//                yield return testCase;

//                yield return new TestCase("4")
//                {
//                    BufferSize = 3,
//                    Source = testCase.Source,
//                    ExpectedResult = testCase.ExpectedResult
//                };

//                yield return new TestCase("5")
//                {
//                    BufferSize = 5,
//                    Source = testCase.Source,
//                    ExpectedResult = testCase.ExpectedResult
//                };

//                testCase = new TestCase("6")
//                {
//                    BufferSize = 4,
//                    Source = BytesOf(new[]
//                    {
//                        "111111.a",
//                        "111111.a",
//                        "111111.a",
//                        "111111.a",
//                        "111111.a",
//                        "111111.a",
//                        "111111.a"
//                    }, withEndLines: true),
//                    ExpectedResult = new Dictionary<string, byte[]>
//                    {
//                        {
//                            id("a"), BytesOf(new[]
//                            {
//                                s(0, 6) + "111111" + s(1) + "a",
//                                s(0, 6) + "111111" + s(1) + "a",
//                                s(0, 6) + "111111" + s(1) + "a",
//                                s(0, 6) + "111111" + s(1) + "a",
//                                s(0, 6) + "111111" + s(1) + "a",
//                                s(0, 6) + "111111" + s(1) + "a",
//                                s(0, 6) + "111111" + s(1) + "a"
//                            }, withEndLines: false)
//                        }
//                    }
//                };

//                yield return testCase;

//                yield return new TestCase("7")
//                {
//                    BufferSize = 5,
//                    Source = testCase.Source,
//                    ExpectedResult = testCase.ExpectedResult
//                };

//                yield return new TestCase("8")
//                {
//                    BufferSize = 12,
//                    Source = testCase.Source,
//                    ExpectedResult = testCase.ExpectedResult
//                };

//                testCase = new TestCase("9")
//                {
//                    BufferSize = 3,
//                    Source = BytesOf(new[]
//                    {
//                        "1.a",
//                        "1.b",
//                        "1.c",
//                        "1.d",
//                        "1.e",
//                        "1.f",
//                        "1.g"
//                    }, withEndLines: true),
//                    ExpectedResult = new Dictionary<string, byte[]>
//                    {
//                        { id("a"), BytesOf(new[] { s(0, 1) + "1" + s(1) + "a" }, false) },
//                        { id("b"), BytesOf(new[] { s(0, 1) + "1" + s(1) + "b" }, false) },
//                        { id("c"), BytesOf(new[] { s(0, 1) + "1" + s(1) + "c" }, false) },
//                        { id("d"), BytesOf(new[] { s(0, 1) + "1" + s(1) + "d" }, false) },
//                        { id("e"), BytesOf(new[] { s(0, 1) + "1" + s(1) + "e" }, false) },
//                        { id("f"), BytesOf(new[] { s(0, 1) + "1" + s(1) + "f" }, false) },
//                        { id("g"), BytesOf(new[] { s(0, 1) + "1" + s(1) + "g" }, false) }
//                    }
//                };

//                yield return testCase;

//                yield return new TestCase("10")
//                {
//                    BufferSize = 4,
//                    Source = testCase.Source,
//                    ExpectedResult = testCase.ExpectedResult
//                };

//                yield return new TestCase("11")
//                {
//                    BufferSize = 7,
//                    Source = testCase.Source,
//                    ExpectedResult = testCase.ExpectedResult
//                };

//                testCase = new TestCase("12")
//                {
//                    BufferSize = 8,
//                    Source = BytesOf(new[]
//                    {
//                        "1.a'''''''''''''''''",
//                        "1.b'''''''''''''''''",
//                        "1.c'''''''''''''''''",
//                        "1.d'''''''''''''''''",
//                        "1.e'''''''''''''''''",
//                        "1.f'''''''''''''''''",
//                        "1.g'''''''''''''''''"
//                    }, withEndLines: true),
//                    ExpectedResult = new Dictionary<string, byte[]>
//                    {
//                        { id("a'"), BytesOf(new[] { s(0, 1) + "1" + s(18) + "a'''''''''''''''''" }, false) },
//                        { id("b'"), BytesOf(new[] { s(0, 1) + "1" + s(18) + "b'''''''''''''''''" }, false) },
//                        { id("c'"), BytesOf(new[] { s(0, 1) + "1" + s(18) + "c'''''''''''''''''" }, false) },
//                        { id("d'"), BytesOf(new[] { s(0, 1) + "1" + s(18) + "d'''''''''''''''''" }, false) },
//                        { id("e'"), BytesOf(new[] { s(0, 1) + "1" + s(18) + "e'''''''''''''''''" }, false) },
//                        { id("f'"), BytesOf(new[] { s(0, 1) + "1" + s(18) + "f'''''''''''''''''" }, false) },
//                        { id("g'"), BytesOf(new[] { s(0, 1) + "1" + s(18) + "g'''''''''''''''''" }, false) }
//                    }
//                };

//                yield return testCase;

//                yield return new TestCase("13")
//                {
//                    BufferSize = 9,
//                    Source = testCase.Source,
//                    ExpectedResult = testCase.ExpectedResult
//                };

//                yield return new TestCase("14")
//                {
//                    BufferSize = 10,
//                    Source = testCase.Source,
//                    ExpectedResult = testCase.ExpectedResult
//                };

//                yield return new TestCase("15")
//                {
//                    BufferSize = 11,
//                    Source = testCase.Source,
//                    ExpectedResult = testCase.ExpectedResult
//                };

//                yield return new TestCase("16")
//                {
//                    BufferSize = 21,
//                    Source = testCase.Source,
//                    ExpectedResult = testCase.ExpectedResult
//                };
                
//                testCase = new TestCase("17")
//                {
//                    BufferSize = 4,
//                    Source = BytesOf(new[]
//                    {
//                        "123.a",
//                        "11.bb",
//                        "11111111.",
//                        "132323.d",
//                        "133.",
//                        "1.bbccc",
//                        "12.g"
//                    }, withEndLines: true),
//                    ExpectedResult = new Dictionary<string, byte[]>
//                    {
//                        {
//                            id("a"), BytesOf(new[]
//                                {
//                                    s(0, 3) + "123" + s(1) + "a"
//                                }, withEndLines: false)
//                        },
//                        {
//                            id("bb"), BytesOf(new[]
//                                {
//                                    s(0, 2) + "11" + s(2) + "bb",
//                                    s(0, 1) + "1"  + s(5) + "bbccc"
//                                }, withEndLines: false)
//                        },
//                        {
//                            id(""), BytesOf(new[]
//                                {
//                                    s(0, 8) + "11111111" + s(0),
//                                    s(0, 3) + "133" + s(0)
//                                }, withEndLines: false)
//                        },
//                        {
//                            id("d"), BytesOf(new[]
//                                {
//                                    s(0, 6) + "132323" + s(1) + "d"
//                                }, withEndLines: false)
//                        },
//                        {
//                            id("g"), BytesOf(new[]
//                                {
//                                    s(0, 2) + "12" + s(1) + "g"
//                                }, withEndLines: false)
//                        }
//                    }
//                };

//                yield return testCase;

//                yield return new TestCase("18")
//                {
//                    BufferSize = 5,
//                    Source = testCase.Source,
//                    ExpectedResult = testCase.ExpectedResult
//                };

//                yield return new TestCase("19")
//                {
//                    BufferSize = 11,
//                    Source = testCase.Source,
//                    ExpectedResult = testCase.ExpectedResult
//                };
//            }
//        }
//    }
//}
