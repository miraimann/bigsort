using System.Collections.Generic;

namespace Bigsort.Tests
{
    public class MappedTestCase
    {
        public InputOutputTestCase FilesSource { get; private set; }

        public IList<long> LinesStarts { get; private set; }

        public int[] LinesOrdering { get; private set; }
        
        public static MappedTestCase Parse(Seed seed) =>
            new MappedTestCase
            {
                FilesSource = InputOutputTestCase.Parse(seed.FilesSource),
                LinesStarts = seed.LineStars,
                LinesOrdering = seed.LinesOrdering
            };

        public class Seed
        {
            private readonly string _name;
            public Seed(string name)
            {
                _name = name;
            }

            public Seed()
                : this("-")
            {
            }

            public InputOutputTestCase.Seed FilesSource { get; set; }

            public IList<long> LineStars { get; set; }

            public int[] LinesOrdering { get; set; }

            public override string ToString() =>
                _name;
        }
    }
}
