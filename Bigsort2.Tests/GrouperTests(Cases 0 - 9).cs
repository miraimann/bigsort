using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bigsort2.Tests
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
                                s(0, 3) + "111" + s(19) + "aa~~~~~~~~~~~~~~~~~",
                                s(0, 3) + "111" + s(08) + "aa~~~~~~",
                                s(0, 3) + "111" + s(30) + "aa----------------------------",
                                s(0, 3) + "111" + s(22) + "aa--------------------"
                            }, withEndLines: false)
                        },
                        {
                            id("ab"), BytesOf(new[]
                            {
                                s(0, 3) + "111" + s(11) + "ab~~~~~~~~~",
                                s(0, 3) + "111" + s(15) + "ab-------------"
                            }, withEndLines: false)
                        }
                    }
                };

                yield return testCase;

                yield return new TestCase("1")
                {
                    BufferSize = 25,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };

                yield return new TestCase("2")
                {
                    BufferSize = 14,
                    Source = testCase.Source,
                    ExpectedResult = testCase.ExpectedResult
                };
            }
        }
    }
}
