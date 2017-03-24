using System;
using System.IO;

namespace Bigsort
{
    public static class BigSorter
    {
        private static Lazy<IoC> _ioC = 
            new Lazy<IoC>(() => new IoC());

        public static void SetLog(TextWriter logger) =>
            _ioC = new Lazy<IoC>(() => new IoC(logger));

        public static void Sort(string inputFilePath, string outputFilePath) =>
            _ioC.Value.LinesSorter.Sort(inputFilePath, outputFilePath);
    }
}
