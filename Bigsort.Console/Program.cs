using System.IO;

namespace Bigsort.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                System.Console.WriteLine("Invalid arguments count");
                return;
            }

            if (!File.Exists(args[0]))
            {
                System.Console.WriteLine("Invalid input file path");
                return;
            }

            BigSorter.SetLog(System.Console.Out);
            BigSorter.Sort(
                inputFilePath: args[0], 
                outputFilePath: args[1]);
        }
    }
}
