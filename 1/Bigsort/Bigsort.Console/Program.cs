using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bigsort.Lib;

namespace Bigsort.Console
{
    using C = System.Console;

    class Program
    {
        static unsafe void Main(string[] args)
        {
            #region research 3

            var t = DateTime.Now;
            using (var stream = new FileStream("E:\\100_Gb.txt", FileMode.Create))
                stream.SetLength(100L * 1024 * 1024 * 1024);

            C.WriteLine(DateTime.Now - t);
            C.ReadKey();

            #endregion

            #region research 2

            //byte[] oooooooa = "oooooooa".Select(o => (byte)o).ToArray(),
            //       ooooooob = "ooooooob".Select(o => (byte)o).ToArray(),
            //       booooooa = "booooooa".Select(o => (byte)o).ToArray(),
            //       aoooooob = "aoooooob".Select(o => (byte)o).ToArray();

            //ulong oooooooaLong = BitConverter.ToUInt64(oooooooa, 0),
            //      ooooooobLong = BitConverter.ToUInt64(ooooooob, 0),
            //      booooooaLong = BitConverter.ToUInt64(oooooooa, 0),
            //      aoooooobLong = BitConverter.ToUInt64(ooooooob, 0);

            //fixed (byte* booooooaPtr = booooooa)
            //fixed (byte* aoooooobPtr = aoooooob)
            //{
            //    ulong* booooooaLongPtr = (ulong*)booooooaPtr;
            //    ulong* aoooooobLongPtr = (ulong*)aoooooobPtr;

            //    var r = Comparer<ulong>.Default.Compare(
            //        *booooooaLongPtr,
            //        *aoooooobLongPtr);
            //}


            //var random = new Random();
            //byte[] bytes = new byte[sizeof(ulong) * 1024];
            //random.NextBytes(bytes);

            //ulong[] direct = new ulong[1024],
            //      reversed = new ulong[1024];

            //for (int i = 0; i < bytes.Length; i += sizeof(ulong))
            //    direct[i / sizeof(ulong)] = BitConverter.ToUInt64(bytes, i);

            //fixed (byte* bytesPtr = bytes)
            //{
            //    ulong* longsPtr = (ulong*) bytesPtr;
            //    for (int i = 0; i < bytes.Length; i += sizeof(ulong), longsPtr++)
            //        reversed[i / sizeof(ulong)] = *longsPtr;
            //}

            // for (int i = 0; i < bytes.Length; i += sizeof(ulong))
            // {
            //     //Array.Reverse(bytes, i, sizeof(ulong));
            //     reversed[i / sizeof(ulong)] = ~BitConverter.ToUInt64(bytes, i);
            //     //Array.Reverse(bytes, i, sizeof(ulong));
            // }

            //Array.Sort(direct);
            //Array.Sort(reversed, Comparer<ulong>.Create((a, b) =>
            //        Comparer<ulong>.Default.Compare(a, b) * -1));

            //byte[] directBytes = direct.SelectMany(BitConverter.GetBytes).ToArray(),
            //       reversedBytes = reversed.SelectMany(BitConverter.GetBytes).ToArray();

            //for (int i = 0; i < bytes.Length; i++)
            //    reversedBytes[i] = (byte)~reversedBytes[i];

            //var result = Enumerable
            //    .Zip(directBytes, reversedBytes,
            //        (a, b) => a == b)
            //    .All(x => x);

            //C.WriteLine(result);
            //C.ReadKey();

            #endregion

            #region research 1

            //byte[] buff = new byte[1024];

            //var t = DateTime.Now;
            //for (int i = 0; i < 10000; i++)
            //    using (var stream = File.OpenWrite($"E:\\1\\{i}"))
            //    {
            //        C.Write('.');
            //        stream.Write(buff, 0, buff.Length);
            //    }

            //var _1 = DateTime.Now - t;
            //t = DateTime.Now;

            //Parallel.For(0, 10000, i =>
            //{
            //    using (var stream = File.OpenWrite($"E:\\2\\{i}"))
            //    {
            //        C.Write('.');
            //        stream.Write(buff, 0, buff.Length);
            //    }
            //});

            //var _2 = DateTime.Now - t;

            //C.Clear();
            //C.WriteLine("1: {0}", _1);
            //C.WriteLine("2: {0}", _2);
            //C.ReadKey();

            #endregion

            //if (args.Length != 2)
            //{
            //    C.WriteLine("Invalid arguments count");
            //    return;
            //}

            //if (!File.Exists(args[0]))
            //{
            //    C.WriteLine("Invalid input file path");
            //    return;
            //}

            //BigSorter.SetLog(C.Out);
            //BigSorter.Sort(
            //    inputFilePath: args[0], 
            //    outputFilePath: args[1]);
        }
    }
}
