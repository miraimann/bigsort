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
    public class LinesIndexatorTests
    {
        private static readonly byte[] Chars;

        static LinesIndexatorTests()
        {
            Chars = new byte[('Z' - 'A') + ('z' - 'a') + 2];
            for (byte x = (byte)'A', i = 0; x <= 'Z'; x++, i++)
                Chars[i] = x;
            for (byte x = (byte)'a', i = 'Z' - 'A'; x <= 'z'; x++, i++)
                Chars[i] = x;
        }

        private Mock<IConfig> _config;
        private ILinesIndexator _indexerator;

        [SetUp]
        public void SetUp()
        {
            _config = new Mock<IConfig>();

            _config.SetupGet(o => o.EndLine)
                   .Returns(Consts.EndLineBytes);
            _config.SetupGet(o => o.Dot)
                   .Returns(Consts.Dot);

            _indexerator = new LinesIndexator(_config.Object);
        }

        [TestCase("", "", "")]
        [TestCase("1.1", "0", "1")]
        [TestCase("1.0", "0", "1")]
        [TestCase("2.1", "0", "2")]
        [TestCase("3.1", "0", "3")]
        [TestCase("8.1", "0", "8")]
        [TestCase("32.32", "0", "32")]
        [TestCase("123.1", "0", "123")]
        [TestCase("123.0", "0", "123")]
        [TestCase("4565.32", "0", "4565")]

        [TestCase("3.1 2.2", "0 7", "3 2")]
        [TestCase("33.1 2.2", "0 37", "33 2")]
        [TestCase("33.11 2.2", "0 47", "33 2")]
        [TestCase("33.11 22.2", "0 47", "33 22")]

        [TestCase("1.1 1.1 1.1 1.1 1.1", "0 5 10 15 20", "1 1 1 1 1")]
        [TestCase("1.0 1.0 1.0 1.0 1.0", "0 4 8 12 16", "1 1 1 1 1")]
        [TestCase("2.0 2.0 2.0 2.0 2.0", "0 5 10 15 20", "2 2 2 2 2")]
        [TestCase("1.0 2.0 3.0 4.0 5.0", "0 4 9 15 22", "1 2 3 4 5")]
        [TestCase("1.5 2.4 3.3 4.2 5.1", "0 9 18 27 36", "1 2 3 4 5")]
        [TestCase("5.1 4.2 3.3 2.4 1.5", "0 9 18 27 36", "5 4 3 2 1")]

        [TestCase("55.1 44.2 33.3 22.4 11.5", "0 59 108 147 176", "55 44 33 22 11")]
        [TestCase("55.11 44.22 33.33 22.44 11.55", "0 69 138 207 276", "55 44 33 22 11")]

        public void Test(
            string inputTemplate, 
            string expectedStartsSource, 
            string expectedDotsShiftsSource)
        {
            var expectedStarts = expectedStartsSource
                .Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)
                .Select(long.Parse)
                .ToArray();

            var expectedDotsShifts = expectedDotsShiftsSource
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToArray();
            
            var starts = new List<long>();
            var dotsShifts = new List<int>();

            _indexerator.IndexLines(
                GenerateInput(inputTemplate),
                starts.Add,
                dotsShifts.Add);

            CollectionAssert.AreEqual(expectedStarts, starts);
            CollectionAssert.AreEqual(expectedDotsShifts, dotsShifts);
        }

        private IEnumerable<byte> GenerateInput(string source)
        {
            var random = new Random();
            var acc = string.Empty;

            foreach (char x in source)
            {
                if (char.IsDigit(x))
                    acc += x;
                else if (x == '.' || x == ' ')
                {
                    var count = int.Parse(acc);
                    acc = string.Empty;
                    for (int j = 0; j < count; j++)
                        yield return Chars[random.Next() % Chars.Length];

                    if (x == '.') yield return (byte) '.';
                    else foreach (var y in Consts.EndLineBytes)
                            yield return y;
                }
            }
        }
    }
}
