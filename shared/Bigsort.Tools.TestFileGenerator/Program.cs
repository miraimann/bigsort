using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bigsort.Implementation;

namespace Bigsort.Tools.TestFileGenerator
{
    class Program
    {
        static int Main(string[] args)
        {
            var n = 10000;
            byte[] buff = new byte[n];
            new Random().NextBytes(buff);

            var t = DateTime.Now;
            for (int i = 0; i < n; i++)
                using (var stream = File.OpenWrite($"E:\\x\\{i}"))
                    stream.Write(buff, 0, n);
            Console.WriteLine($"X: {DateTime.Now - t}");

            t = DateTime.Now;
            Parallel.For(0, n, i =>
            {
                using (var stream = File.OpenWrite($"E:\\y\\{i}"))
                    stream.Write(buff, 0, n);
            });
            Console.WriteLine($"Y: {DateTime.Now - t}");
            
            t = DateTime.Now;
            Parallel.For(0, n, i =>
            {
                using (var stream = File.OpenWrite($"E:\\y\\{i}"))
                    stream.Write(buff, 0, n);
            });
            Console.WriteLine($"Y: {DateTime.Now - t}");

            var option = new ParallelOptions
            {
                MaxDegreeOfParallelism = 4
            };

            t = DateTime.Now;
            Parallel.For(0, n, option, i =>
            {
                using (var stream = File.OpenWrite($"E:\\z\\{i}"))
                    stream.Write(buff, 0, n);
            });
            Console.WriteLine($"Z: {DateTime.Now - t}");


            option = new ParallelOptions
            {
                MaxDegreeOfParallelism = 2
            };

            t = DateTime.Now;
            Parallel.For(0, n, option, i =>
            {
                using (var stream = File.OpenWrite($"E:\\w\\{i}"))
                    stream.Write(buff, 0, n);
            });
            Console.WriteLine($"w: {DateTime.Now - t}");

            option = new ParallelOptions
            {
                MaxDegreeOfParallelism = 1
            };

            t = DateTime.Now;
            Parallel.For(0, n, option, i =>
            {
                using (var stream = File.OpenWrite($"E:\\q\\{i}"))
                    stream.Write(buff, 0, n);
            });
            Console.WriteLine($"q: {DateTime.Now - t}");


            Console.ReadKey();
            return 0;

            // if (args.Length != 3)
            // {
            //     Console.WriteLine("invalid args");
            //     return 1;
            // }
            // 
            // Generator.Generate(args[0], args[1], args[2]);
            // 
            // return 0;
        }
    }
}
