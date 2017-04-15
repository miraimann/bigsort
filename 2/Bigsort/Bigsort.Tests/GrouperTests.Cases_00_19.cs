using System.Collections.Generic;

namespace Bigsort.Tests
{
    public partial class GrouperTests
    {
        // ReSharper disable once InconsistentNaming
        public static IEnumerable<TestCase> Cases_00_19
        {
            get
            {
                yield return new TestCase("00", new[]
                {
                    "111.ab~~~~~~~~~",
                    "111.aa~~~~~~~~~~~~~~~~~",
                    "111.aa~~~~~~",
                    "111.ab-------------",
                    "111.aa----------------------------",
                    "111.aa--------------------"
                });
            }
        }
    }
}
