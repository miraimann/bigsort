using System.IO;
using Bigsort.Lib;

namespace Bigsort.Console
{
    using C = System.Console;

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                C.WriteLine("Invalid arguments count");
                return;
            }
            
            if (!File.Exists(args[0]))
            {
                C.WriteLine("Invalid input file path");
                return;
            }

            BigSorter.SetLog(C.Out);
            BigSorter.Sort(
                inputFilePath: args[0], 
                outputFilePath: args[1]);
        }
    }
}
