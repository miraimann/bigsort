using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices;
using System.Text;

namespace Bigsort.Console
{
    using C = System.Console;

    #region research 8

    class DisposableFactory
    {
        public IDisposable Make(Action action) =>
            new Disposable(action);

        private class Disposable : IDisposable
        {
            private readonly Action _action;

            public Disposable(Action action)
            {
                _action = action;
            }

            public void Dispose() =>
                _action();
        }
    }

    #endregion

    class Program
    {
        static unsafe void Main(string[] args)
        {
            #region research 9

            //byte[] buff = {1, 2, 3, 4, 5, 6, 7, 8};
            //for (char i = ' '; i <= '~'; ++i)
            //    try
            //    {
            //        using (var stream = File.OpenWrite($"E:\\{i}"))
            //            stream.Write(buff, 0, buff.Length);
            //    }
            //    catch (Exception)
            //    {
            //        C.Write(i);
            //    }

            //C.ReadKey();

            #endregion

            #region research 8

            //var f = new DisposableFactory();
            //IDisposable x = null;
            //using (x)
            //{
            //    x = f.Make(() =>
            //    {
            //        C.WriteLine("!!!!");
            //        C.ReadKey();
            //    });
            //}

            #endregion

            #region research 7

            //using (var stream = File.OpenWrite("E:\\x"))
            //{
            //    stream.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7}, 0, 6);
            //    stream.Position -= 3;
            //    stream.WriteByte(9);
            //}

            //using (var stream = File.OpenRead("E:\\x"))
            //{
            //    var buff = new byte[6];
            //    stream.Read(buff, 0, 6);
            //    foreach (var x in buff)
            //        C.WriteLine(x);
            //}

            //File.Delete("E:\\x");
            //C.ReadKey();

            #endregion

            #region research 6

            //C.WriteLine(23.ToString("000"));
            //C.ReadKey();

            #endregion

            #region research 5

            //const int fileSize = 1024 * 1024,
            //          filesCount = 10000;

            //var random = new Random();
            //var bytes = new byte[fileSize];
            //random.NextBytes(bytes);

            //var t = DateTime.Now;
            //for (int i = 0; i < filesCount; i++)
            //    using (var stream = File.OpenWrite("E:\\" + i))
            //    {
            //        C.Write(".");
            //        if (i % 100 == 99)
            //            C.WriteLine();

            //        stream.Write(bytes, 0, fileSize);
            //    }

            //C.WriteLine(DateTime.Now - t);
            //C.ReadKey();

            #endregion

            #region research 4

            //byte[] _1 = { 1, 0, 0, 0, 0, 0, 0, 0 };
            //byte[] _2 = { 0, 0, 0, 0, 0, 0, 0, 1 };

            //fixed (byte* _1ptr = _1, _2ptr = _2)
            //{
            //    long* _1longPtr = (long*)_1ptr,
            //          _2longPtr = (long*)_2ptr;

            //    C.WriteLine(*_1longPtr);
            //    C.WriteLine(*_2longPtr);
            //    C.ReadKey();
            //}

            #endregion

            #region research 3

            //C.WriteLine(BitConverter.ToInt64(
            //    new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 }, 0));

            //C.WriteLine(BitConverter.ToInt64(
            //    new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }, 0));

            //C.WriteLine(BitConverter.ToInt64(
            //    new byte[] { 1, 2, 0, 0, 0, 0, 0, 0 }, 0));

            //C.WriteLine(BitConverter.ToInt64(
            //    new byte[] { 2, 1, 0, 0, 0, 0, 0, 0 }, 0));

            //C.WriteLine(BitConverter.ToInt64(
            //    new byte[] { 0, 0, 0, 0, 0, 0, 1, 2 }, 0));

            //C.ReadKey();

            #endregion

            #region research 3

            //var n = int.MaxValue/100;
            //var random = new Random();
            //byte[] sorce = new byte[n],
            //      target = new byte[n];

            //random.NextBytes(sorce);

            //var t = DateTime.Now;
            //for (int i = 0; i < n; i++)
            //    target[i] = sorce[i];
            //C.WriteLine(DateTime.Now - t);

            //t = DateTime.Now;
            //Array.Copy(sorce, target, n);
            //C.WriteLine(DateTime.Now - t);

            //C.ReadKey();

            #endregion

            #region research 2

            //byte _0 = (byte) '0',
            //     _1 = (byte) '1',
            //     _9 = (byte) '9',
            //    dot = (byte) '.',
            //    end1 = (byte) '\n',
            //    end2 = (byte) '\r',
            //    merge = (byte)(((_1 - _0) << 4) | (_9 - _0));

            //C.WriteLine("'0'          : {0}", Convert.ToString(_0, 2));
            //C.WriteLine("'1'          : {0}", Convert.ToString(_1, 2));
            //C.WriteLine("'9'          : {0}", Convert.ToString(_9, 2));
            //C.WriteLine("'9' merge '1': {0}", Convert.ToString(merge, 2));
            //C.WriteLine("'.'          : {0}", Convert.ToString(dot, 2));
            //C.WriteLine("'\\n'         : {0}", Convert.ToString(end1, 2));
            //C.WriteLine("'\\r'         : {0}", Convert.ToString(end2, 2));
            //C.WriteLine("' '          : {0}", Convert.ToString((byte)' ', 2));
            //C.WriteLine("'~'          : {0}", Convert.ToString((byte)'~', 2));
            //C.ReadLine();

            #endregion

            #region research 1

            //string
            //    line1 = "bbcbexab",
            //    line2 = "abcaexbb";

            //using (var s = File.OpenWrite("C:\\x.txt"))
            //using (var writer = new StreamWriter(s, Encoding.ASCII))
            //{
            //    writer.Write(line1);
            //    writer.Write(line2);
            //}

            //var buff = new byte[sizeof(long) * 2];
            //using (var s = File.OpenRead("C:\\x.txt"))
            //    s.Read(buff, 0, sizeof(long) * 2);

            //var long1 = BitConverter.ToInt64(buff, 0);
            //var long2 = BitConverter.ToInt64(buff, sizeof(long));

            //C.WriteLine(Comparer<long>.Default.Compare(long1, long2));

            //File.Delete("C:\\x.txt");
            //C.ReadKey();

            #endregion

            // if (args.Length != 2)
            // {
            //     System.Console.WriteLine("Invalid arguments count");
            //     return;
            // }
            // 
            // if (!File.Exists(args[0]))
            // {
            //     System.Console.WriteLine("Invalid input file path");
            //     return;
            // }

            // BigSorter.SetLog(System.Console.Out);
            // BigSorter.Sort(
            //     inputFilePath: args[0], 
            //     outputFilePath: args[1]);
        }
    }
}
