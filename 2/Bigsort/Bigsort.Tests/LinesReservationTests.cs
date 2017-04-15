//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;
//using Bigsort.Contracts;
//using Bigsort.Implementation;
//using Moq;
//using NUnit.Framework;
//using static Bigsort.Tests.Tools;
//using Range = Bigsort.Contracts.Range;

//namespace Bigsort.Tests
//{
//    [TestFixture]
//    public class LinesReservationTests
//    {
//        private const int ArrayLength = 20;
//        private IUsingHandleMaker _disposableValueMaker;
//        private Mock<IConfig> _configMock; 
//        private ILinesReservation<int> _reservation;
        
//        [SetUp]
//        public void Setup()
//        {
//            _configMock = new Mock<IConfig>();

//            var itemSize = sizeof(int) 
//                         + Marshal.SizeOf<LineIndexes>();
//            _configMock
//                .SetupGet(o => o.MaxMemoryForLines)
//                .Returns(ArrayLength * itemSize);

//            _disposableValueMaker = new UsingHandleMaker();
//            _reservation = new LinesReservation<int>(
//                _disposableValueMaker,
//                _configMock.Object);

//            _reservation.Load();
//        }

//        [Test]
//        public void LengthCalculationTest()
//        {
//            Assert.AreEqual(ArrayLength, _reservation.Length);
//            Assert.AreEqual(ArrayLength, _reservation.Indexes.Length);
//            Assert.AreEqual(ArrayLength, _reservation.Segments.Length);
//        }

//        [Test]
//        public void Test1()
//        {
//            Check(_reservation.TryReserveRange(3), 0, 3);
//            Check(_reservation.TryReserveRange(2), 3, 2);
//            Check(_reservation.TryReserveRange(3), 5, 3);
//            Check(_reservation.TryReserveRange(2), 8, 2);
//            Check(_reservation.TryReserveRange(10), 10, 10);
//            Assert.IsNull(_reservation.TryReserveRange(1));
//        }

//        [Test]
//        public void Test2()
//        {
//            Check(_reservation.TryReserveRange(3), 0, 3);
//            Check(_reservation.TryReserveRange(2), 3, 2);

//            var x = _reservation.TryReserveRange(3);
//            Check(x, 5, 3);
//            x.Dispose();

//            Check(_reservation.TryReserveRange(2), 5, 2);
//            Check(_reservation.TryReserveRange(2), 7, 2);
//        }

//        [Test]
//        public void Test3()
//        {
//            var a = _reservation.TryReserveRange(3);
//            Check(a, 0, 3);

//            Check(_reservation.TryReserveRange(2), 3, 2);
//            Check(_reservation.TryReserveRange(2), 5, 2);

//            a.Dispose();

//            Check(_reservation.TryReserveRange(3), 0, 3);
//            Check(_reservation.TryReserveRange(2), 7, 2);
//        }

//        [Test]
//        public void Test4()
//        {
//            Check(_reservation.TryReserveRange(3), 0, 3);

//            var b = _reservation.TryReserveRange(2);
//            Check(b, 3, 2);

//            var c = _reservation.TryReserveRange(2);
//            Check(c, 5, 2);

//            Check(_reservation.TryReserveRange(2), 7, 2);

//            b.Dispose();
//            c.Dispose();

//            Check(_reservation.TryReserveRange(4), 3, 4);
//        }

//        [Test]
//        public void Test5()
//        {
//            Check(_reservation.TryReserveRange(3), 0, 3);

//            var b = _reservation.TryReserveRange(2);
//            Check(b, 3, 2);

//            var c = _reservation.TryReserveRange(3);
//            Check(c, 5, 3);

//            Check(_reservation.TryReserveRange(2), 8, 2);

//            c.Dispose();
//            b.Dispose();

//            Check(_reservation.TryReserveRange(5), 3, 5);
//        }

//        [Test]
//        public void Test6()
//        {
//            Check(_reservation.TryReserveRange(2), 0, 2);

//            var a = _reservation.TryReserveRange(3);
//            Check(a, 2, 3);

//            var b = _reservation.TryReserveRange(2);
//            Check(b, 5, 2);

//            var c = _reservation.TryReserveRange(3);
//            Check(c, 7, 3);

//            Check(_reservation.TryReserveRange(2), 10, 2);

//            a.Dispose();
//            c.Dispose();
//            b.Dispose();

//            Check(_reservation.TryReserveRange(7), 2, 7);
//        }

//        [Test]
//        public void Test(
//            [Values(
//                "00. +a|3|0, +b|4|3, -a, +с|3|0",
//                "01. +a|3|0, +b|4|3, -b, +с|3|3",
                   
//                "02. +a|1|0, +b|1|1, +c|1|2, -b, +d|1|1",
//                "03. +a|1|0, +b|1|1, +c|1|2, -a, +d|1|0",
//                "04. +a|1|0, +b|1|1, +c|1|2, -c, +d|1|2",
//                "05. +a|1|0, +b|1|1, +c|1|2, -a, -b, +d|2|0",
//                "06. +a|1|0, +b|1|1, +c|1|2, +d|1|3, -b, -c, +e|2|1",
//                "07. +a|1|0, +b|1|1, +c|1|2, +d|1|3, -c, -b, +e|2|1",
                   
//                "08. +a|2|0, +b|2|2, +c|2|4, -b, +d|2|2",
//                "09. +a|2|0, +b|2|2, +c|2|4, -a, +d|2|0",
//                "10. +a|2|0, +b|2|2, +c|2|4, -c, +d|2|4",
//                "11. +a|2|0, +b|2|2, +c|2|4, -b, +d|1|2",
//                "12. +a|2|0, +b|2|2, +c|2|4, -a, +d|1|0",
//                "13. +a|2|0, +b|2|2, +c|2|4, -c, +d|1|4",
//                "14. +a|2|0, +b|2|2, +c|2|4, -b, +d|3|6",
//                "15. +a|2|0, +b|2|2, +c|2|4, -a, +d|3|6",
//                "16. +a|2|0, +b|2|2, +c|2|4, -c, +d|3|4",
                   
//                "17. +a|2|0, +b|2|2, +c|2|4, -a, -b, +d|4|0",
//                "18. +a|2|0, +b|2|2, +c|2|4, +d|2|6, -b, -c, +e|4|2",
//                "19. +a|2|0, +b|2|2, +c|2|4, +d|2|6, -c, -b, +e|4|2",
//                "20. +a|2|0, +b|2|2, +c|2|4, -a, -b, +d|3|0",
//                "21. +a|2|0, +b|2|2, +c|2|4, +d|2|6, -b, -c, +e|3|2",
//                "22. +a|2|0, +b|2|2, +c|2|4, +d|2|6, -c, -b, +e|3|2",
//                "23. +a|2|0, +b|2|2, +c|2|4, -a, -b, +d|5|6",
//                "24. +a|2|0, +b|2|2, +c|2|4, +d|2|6, -b, -c, +e|5|8",
//                "25. +a|2|0, +b|2|2, +c|2|4, +d|2|6, -c, -b, +e|5|8",
                   
//                "26. +a|2|0, +b|2|2, +c|2|4, -c, -b, +x|2|2, +y|2|4",
//                "27. +a|2|0, +b|2|2, +c|2|4, -b, -c, +x|2|2, +y|2|4",
//                "28. +a|2|0, +b|2|2, +c|2|4, -a, -b, +x|2|0, +y|2|2",
//                "29. +a|2|0, +b|2|2, +c|2|4, -b, -a, +x|2|0, +y|2|2",
//                "30. +a|2|0, +b|2|2, +c|2|4, -a, -c, +x|2|0, +y|2|4",
//                "31. +a|2|0, +b|2|2, +c|2|4, -c, -a, +x|2|0, +y|2|4",
                   
//                "32. +a|2|0, +b|2|2, +c|2|4, -a, -b, -c, +x|2|0, +y|2|2, +z|2|4",
//                "33. +a|2|0, +b|2|2, +c|2|4, -a, -c, -b, +x|2|0, +y|2|2, +z|2|4",
//                "34. +a|2|0, +b|2|2, +c|2|4, -b, -a, -c, +x|2|0, +y|2|2, +z|2|4",
//                "35. +a|2|0, +b|2|2, +c|2|4, -b, -c, -a, +x|2|0, +y|2|2, +z|2|4",
//                "36. +a|2|0, +b|2|2, +c|2|4, -c, -a, -b, +x|2|0, +y|2|2, +z|2|4",
//                "37. +a|2|0, +b|2|2, +c|2|4, -c, -b, -a, +x|2|0, +y|2|2, +z|2|4",
                   
//                "38. +a|2|0, +b|2|2, +c|2|4, -a, -b, -c, +x|3|0, +y|1|3, +z|2|4",
//                "39. +a|2|0, +b|2|2, +c|2|4, -a, -c, -b, +x|3|0, +y|1|3, +z|2|4",
//                "40. +a|2|0, +b|2|2, +c|2|4, -b, -a, -c, +x|3|0, +y|1|3, +z|2|4",
//                "41. +a|2|0, +b|2|2, +c|2|4, -b, -c, -a, +x|3|0, +y|1|3, +z|2|4",
//                "42. +a|2|0, +b|2|2, +c|2|4, -c, -a, -b, +x|3|0, +y|1|3, +z|2|4",
//                "43. +a|2|0, +b|2|2, +c|2|4, -c, -b, -a, +x|3|0, +y|1|3, +z|2|4",
                   
//                "44. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -d, -b, -c, +x|2|2, +y|2|4, +z|2|6",
//                "45. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -d, -c, -b, +x|2|2, +y|2|4, +z|2|6",
//                "46. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -b, -d, -c, +x|2|2, +y|2|4, +z|2|6",
//                "47. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -b, -c, -d, +x|2|2, +y|2|4, +z|2|6",
//                "48. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -c, -d, -b, +x|2|2, +y|2|4, +z|2|6",
//                "49. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -c, -b, -d, +x|2|2, +y|2|4, +z|2|6",
                   
//                "51. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -d, -b, -c, +x|3|2, +y|2|5, +z|1|7",
//                "52. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -d, -c, -b, +x|3|2, +y|2|5, +z|1|7",
//                "53. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -b, -d, -c, +x|3|2, +y|2|5, +z|1|7",
//                "54. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -b, -c, -d, +x|3|2, +y|2|5, +z|1|7",
//                "55. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -c, -d, -b, +x|3|2, +y|2|5, +z|1|7",
//                "56. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -c, -b, -d, +x|3|2, +y|2|5, +z|1|7",
                   
//                "57. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -b, -d, +x|2|2, +y|2|6, +z|1|10",
//                "58. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -d, -b, +x|2|2, +y|2|6, +z|1|10",
//                "59. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -b, -d, +x|1|2, +y|3|10, +z|2|6, +w|1|3",
//                "60. +a|2|0, +b|2|2, +c|2|4, +d|2|6, +e|2|8, -d, -b, +x|1|2, +y|3|10, +z|2|6, +w|1|3",

//                "61. +a|1|0, +b|2|1, +c|3|3, +d|4|6, +e|5|10, -d, -b, -c, +x|2|1, +y|3|3, +z|4|6",
//                "62. +a|1|0, +b|2|1, +c|3|3, +d|4|6, +e|5|10, -d, -c, -b, +x|2|1, +y|3|3, +z|4|6",
//                "63. +a|1|0, +b|2|1, +c|3|3, +d|4|6, +e|5|10, -b, -d, -c, +x|2|1, +y|3|3, +z|4|6",
//                "64. +a|1|0, +b|2|1, +c|3|3, +d|4|6, +e|5|10, -b, -c, -d, +x|2|1, +y|3|3, +z|4|6",
//                "65. +a|1|0, +b|2|1, +c|3|3, +d|4|6, +e|5|10, -c, -d, -b, +x|2|1, +y|3|3, +z|4|6",
//                "66. +a|1|0, +b|2|1, +c|3|3, +d|4|6, +e|5|10, -c, -b, -d, +x|2|1, +y|3|3, +z|4|6"
//            )] string scenario)
//        {
//            scenario = scenario.Substring(4);
//            var handles = new Dictionary<string, IDisposable>();
//            var commands = SplitString(scenario, ", ");
//            foreach (var cmd in commands)
//            {
//                TestContext.Out?.WriteLine(cmd);
//                if (cmd[0] == '+')
//                {
//                    var parts = SplitString(cmd.Substring(1), "|");
//                    var name = parts[0];
//                    var length = int.Parse(parts[1]);
//                    var offset = int.Parse(parts[2]);
//                    var range = _reservation.TryReserveRange(length);

//                    Check(range, offset, length);
                    
//                    handles.Add(name, range);
//                }
//                else // cmd[0] == '-'
//                {
//                    var name = cmd.Substring(1);
//                    handles[name].Dispose();
//                    handles.Remove(name);
//                }
//            }
//        }

//        private void Check(
//            IUsingHandle<Range> result,
//            int expectedOffset,
//            int expectedLength)
//        {
//            Assert.IsNotNull(result);
//            Assert.AreEqual(expectedOffset, result.Value.Offset);
//            Assert.AreEqual(expectedLength, result.Value.Length);
//        }
//    }
//}
