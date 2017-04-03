using System.Collections.Generic;

namespace Bigsort.Tests
{
    public partial class GroupSorterTests
    {
        public static IEnumerable<TestCase> Cases1
        {
            get
            {
                var testCase = new TestCase("0",
                    inputLines: new TestCase.InputLineList
                    { 
                        { "00|3|4|2|False", "--1234.--g" }, 
                        { "10|3|4|2|False", "--1234.--f" }, 
                        { "20|3|4|2|False", "--1234.--e" }, 
                        { "30|3|4|2|False", "--1234.--d" }, 
                        { "40|3|4|2|False", "--1234.--b" }, 
                        { "50|3|4|2|False", "--1234.--c" }, 
                        { "60|3|4|2|False", "--1234.--a" }  
                    },
                   sortedLines: new [] { 60, 40, 50, 30, 20, 10, 0 },
                    bufferSize: 1024);

                yield return testCase;
                yield return new TestCase("1", testCase, 12);
                yield return new TestCase("2", testCase, 8);

                testCase = new TestCase("3",
                    inputLines: new TestCase.InputLineList
                    { 
                        { "00|4|4|2|False", "--1234.---g" }, 
                        { "11|4|4|2|False", "--1234.---f" }, 
                        { "22|4|4|2|False", "--1234.---e" }, 
                        { "33|4|4|2|False", "--1234.---d" }, 
                        { "44|4|4|2|False", "--1234.---b" }, 
                        { "55|4|4|2|False", "--1234.---c" }, 
                        { "66|4|4|2|False", "--1234.---a" }  
                    },
                   sortedLines: new [] { 66, 44, 55, 33, 22, 11, 00 },
                    bufferSize: 1024);

                yield return testCase;
                yield return new TestCase("4", testCase, 12);
                yield return new TestCase("5", testCase, 8);

                testCase = new TestCase("6",
                    inputLines: new TestCase.InputLineList
                    {
                        { "00|4|8|2|False", "--12345678.---g" },
                        { "15|4|2|2|False", "--12.---f"       },
                        { "24|4|1|2|False", "--1.---e"        },
                        { "32|4|6|2|False", "--123456.---d"   },
                        { "45|4|7|2|False", "--1234567.---b"  },
                        { "59|4|5|2|False", "--12345.---c"    },
                        { "71|4|3|2|False", "--123.---a"      }
                    },
                   sortedLines: new[] { 71, 45, 59, 32, 24, 15, 00 },
                    bufferSize: 1024);

                yield return testCase;
                yield return new TestCase("7", testCase, 12);
                yield return new TestCase("8", testCase, 8);

                testCase = new TestCase("9",
                    inputLines: new TestCase.InputLineList
                    {
                        { "00|8|1|2|False", "--1.////////" },
                        { "12|3|1|2|False", "--1.///"      },
                        { "19|7|1|2|False", "--1.///////"  },
                        { "30|5|1|2|False", "--1./////"    },
                        { "39|6|1|2|False", "--1.//////"   },
                        { "49|2|1|2|False", "--1.//"       },
                        { "55|4|1|2|False", "--1.////"     }
                    },
                   sortedLines: new[] { 49, 12, 55, 30, 39, 19, 00 },
                    bufferSize: 1024);

                yield return testCase;
                yield return new TestCase("10", testCase, 12);
                yield return new TestCase("11", testCase, 8);
                
                testCase = new TestCase("12",
                    inputLines: new TestCase.InputLineList
                    {
                        { "00|5|1|2|False", "--1.--xoo" },
                        { "09|5|1|2|False", "--1.--oox" },
                        { "18|5|1|2|False", "--1.--oxx" },
                        { "27|5|1|2|False", "--1.--ooo" },
                        { "36|5|1|2|False", "--1.--xxo" },
                        { "45|5|1|2|False", "--1.--oxo" },
                        { "54|5|1|2|False", "--1.--xxx" }
                    },
                   sortedLines: new[] { 27, 09, 45, 18, 00, 36, 54 },
                    bufferSize: 1024);

                yield return testCase;
                yield return new TestCase("13", testCase, 12);
                yield return new TestCase("14", testCase, 8);
                yield return new TestCase("15", testCase, 9);

                testCase = new TestCase("16",
                    inputLines: new TestCase.InputLineList
                    {
                        { "000|14|3|2|False", "--222.-----------xoo" },
                        { "020|14|3|2|False", "--222.-----------oox" },
                        { "040|14|3|2|False", "--222.-----------oxx" },
                        { "060|14|3|2|False", "--222.-----------ooo" },
                        { "080|14|3|2|False", "--222.-----------xxo" },
                        { "100|14|3|2|False", "--222.-----------oxo" },
                        { "120|14|3|2|False", "--222.-----------xxx" }
                    },
                   sortedLines: new[] { 060, 020, 100, 040, 000, 080, 120 },
                    bufferSize: 1024);

                yield return testCase;
                yield return new TestCase("17", testCase, 10);
                yield return new TestCase("18", testCase, 8);
                
                testCase = new TestCase("19",
                    inputLines: new TestCase.InputLineList
                    {
                        { "000|24|3|2|False", "--222.---------xoo---------xxx" },
                        { "030|24|3|2|False", "--222.---------oox---------ooo" },
                        { "060|24|3|2|False", "--222.---------oxx---------xxx" },
                        { "090|24|3|2|False", "--222.---------ooo---------ooo" },
                        { "120|24|3|2|False", "--222.---------xxo---------xxx" },
                        { "150|24|3|2|False", "--222.---------oxo---------ooo" },
                        { "180|24|3|2|False", "--222.---------xxx---------xxx" }
                    },
                   sortedLines: new[] { 090, 030, 150, 060, 000, 120, 180 },
                    bufferSize: 1024);

                yield return testCase;
                yield return new TestCase("20", testCase, 10);
                yield return new TestCase("21", testCase, 8);

                testCase = new TestCase("22",
                    inputLines: new TestCase.InputLineList
                    {
                        { "000|14|3|2|False", "--222.--01029-------" }, // 25 
                        { "020|14|3|2|False", "--222.--00150-------" }, // 17
                        { "040|14|3|2|False", "--222.--00130-------" }, // 15
                        { "060|14|3|2|False", "--222.--00140-------" }, // 16
                        { "080|14|3|2|False", "--222.--00020-------" }, // 4
                        { "100|14|3|2|False", "--222.--00012-------" }, // 3
                        { "120|14|3|2|False", "--222.--01011-------" }, // 23
                        
                        { "140|14|3|2|False", "--222.--00181-------" }, // 20
                        { "160|14|3|2|False", "--222.--00171-------" }, // 19
                        { "180|14|3|2|False", "--222.--00191-------" }, // 21
                        { "200|14|3|2|False", "--222.--00010-------" }, // 1
                        { "220|14|3|2|False", "--222.--00161-------" }, // 18
                        { "240|14|3|2|False", "--222.--01109-------" }, // 26
                        { "260|14|3|2|False", "--222.--01118-------" }, // 27
                        
                        { "280|14|3|2|False", "--222.--01010-------" }, // 22
                        { "300|14|3|2|False", "--222.--01020-------" }, // 24
                        { "320|14|3|2|False", "--222.--00110-------" }, // 14
                        { "340|14|3|2|False", "--222.--00106-------" }, // 13
                        { "360|14|3|2|False", "--222.--00041-------" }, // 6
                        { "380|14|3|2|False", "--222.--00011-------" }, // 2
                        { "400|14|3|2|False", "--222.--00030-------" }, // 5
                        
                        { "420|14|3|2|False", "--222.--00009-------" }, // 0
                        { "440|14|3|2|False", "--222.--00105-------" }, // 12
                        { "460|14|3|2|False", "--222.--00104-------" }, // 11
                        { "480|14|3|2|False", "--222.--00102-------" }, // 9
                        { "500|14|3|2|False", "--222.--00100-------" }, // 7
                        { "520|14|3|2|False", "--222.--00101-------" }, // 8
                        { "540|14|3|2|False", "--222.--00103-------" }, // 10
                    },
                   sortedLines: new[]
                   {
                       420, 200, 380, 100, 080, 400, 360,
                       500, 520, 480, 540, 460, 440, 340,
                       320, 040, 060, 020, 220, 160, 140,
                       180, 280, 120, 300, 000, 240, 260
                   },
                    bufferSize: 1024);

                yield return testCase;
                yield return new TestCase("23", testCase, 24);
                yield return new TestCase("24", testCase, 8);

                testCase = new TestCase("25",
                    inputLines: new TestCase.InputLineList
                    {
                        { "000|14|3|2|False", "--222.--////////ooox" }, // 0 
                        { "020|09|3|2|False", "--222.--////xxx"      }, // 10
                        { "035|14|3|2|False", "--222.--////////ooxx" }, // 2
                        { "055|09|3|2|False", "--222.--////xox"      }, // 8
                        { "070|14|3|2|False", "--222.--////////xxxx" }, // 5
                        { "090|09|3|2|False", "--222.--////oxo"      }, // 7
                        { "105|06|3|2|False", "--222.--//oo"         }, // 11
                        
                        { "117|09|3|2|False", "--222.--////xxo"      }, // 9
                        { "132|14|3|2|False", "--222.--////////xoxx" }, // 4
                        { "152|09|3|2|False", "--222.--////xoo"      }, // 6
                        { "167|14|3|2|False", "--222.--////////oxoo" }, // 3
                        { "187|06|3|2|False", "--222.--//ox"         }, // 12
                        { "199|14|3|2|False", "--222.--////////ooxo" }, // 1
                        { "219|06|3|2|False", "--222.--//xo"         }, // 13
                    },
                   sortedLines: new[]
                   {
                       000, 199, 035, 167, 132, 070, 090,
                       152, 055, 117, 020, 105, 187, 219
                   },
                    bufferSize: 1024);

                yield return testCase;
                yield return new TestCase("26", testCase, 17);
                yield return new TestCase("27", testCase, 8);

                yield return testCase = new TestCase("28",
                    inputLines: new TestCase.InputLineList
                    { 
                        { "00|3|4|2|False", "--1234.--a" }, 
                        { "10|3|4|2|False", "--1234.--b" }, 
                    },
                   sortedLines: new [] { 00, 10 },
                    bufferSize: 8);
                
                yield return testCase = new TestCase("29",
                    inputLines: new TestCase.InputLineList
                    {
                        { "00|3|5|2|False", "--1234.--aaa" },
                        { "11|3|4|2|False", "--1234.--aa" },
                    },
                   sortedLines: new[] { 11, 00 },
                    bufferSize: 34);
            }
        }
    }
}