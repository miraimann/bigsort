using System;
using System.IO;
using Bigsort.Tools.SortedFileChecker;
using Bigsort.Tools.TestFileGenerator;
using NUnit.Framework;

namespace Bigsort.Tests
{
    [TestFixture]
    public class RealTests
    {
        [TestCase("1_Mb", "[1].[0]"
            , Ignore = "for run by hands only"
            )]

        [TestCase("1_Mb", "[1-10].[0-20]"
            , Ignore = "for run by hands only"
            )]

        [TestCase("1_Mb", "[1-10].[0-20]"
            , Ignore = "for run by hands only"
            )]
        
        [TestCase("1_Mb", "[100].[100]"
            , Ignore = "for run by hands only"
            )]
        
        [TestCase("1_Mb", "[1-100].[0-200]"
            , Ignore = "for run by hands only"
            )]

        [TestCase("1_Mb", "[10].[10]"
            , Ignore = "for run by hands only"
            )]
        
        [TestCase("100_Mb", "[1-1000].[0-2000]"
            // , Ignore = "for run by hands only"
            )]

        [TestCase("1_Gb", "[1-10000].[0-20000]"
            , Ignore = "for run by hands only"
            )]

        public void Test(string sizeData, string lineSettings)
        {
            const string
                input = "E:\\input.txt",
                output = "E:\\output.txt",
                separator = "          ***          ";

            try
            {
                // Generate
                var start = DateTime.Now;

                Generator.Generate(sizeData, lineSettings, input);

                TestContext.Out?.WriteLine(
                    "generation time: {0}", 
                    DateTime.Now - start
                    );

                // Sort
                start = DateTime.Now;
                TestContext.Out?.WriteLine(separator);

                BigSorter.SetLog(TestContext.Out);
                BigSorter.Sort(input, output);

                TestContext.Out?.WriteLine(separator);
                TestContext.Out?.WriteLine(
                    "sorting time: {0}", 
                    DateTime.Now - start
                    );

                // Check
                start = DateTime.Now;

                Assert.AreEqual(
                    new FileInfo(input).Length,
                    new FileInfo(output).Length);
                Assert.IsTrue(Checker.IsSorted(output));

                TestContext.Out?.WriteLine(
                    "checking time: {0}", 
                    DateTime.Now - start
                    );

            }
            finally
            {
                // Clear
                 foreach (var file in new[] { input, output})
                     if (File.Exists(file))
                         File.Delete(file);
            }
        }
    }
}
