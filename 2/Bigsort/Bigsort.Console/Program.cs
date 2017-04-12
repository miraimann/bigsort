using System;
using Bigsort.Lib;

namespace Bigsort.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var t = DateTime.Now;

            FileLinesSorter
                .Sort(args[0], args[1]);

            System.Console.WriteLine("{0}", DateTime.Now - t);
            System.Console.ReadKey();
        }
    }
}
