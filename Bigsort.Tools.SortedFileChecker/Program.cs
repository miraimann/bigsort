using System;

namespace Bigsort.Tools.SortedFileChecker
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("invalid args");
                return 1;
            }

            Console.WriteLine(Checker.IsSorted(args[0]));
            Console.ReadKey();
            return 0;
        }
    }
}
