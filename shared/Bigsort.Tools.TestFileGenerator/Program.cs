using System;
using System.IO;
using System.Threading.Tasks;

namespace Bigsort.Tools.TestFileGenerator
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("invalid args");
                return 1;
            }
            
            Generator.Generate(args[0], args[1], args[2]);
            
            return 0;
        }
    }
}
