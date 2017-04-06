using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Bigsort.Lib;

namespace Bigsort.Console
{
    using C = System.Console;

    class Program
    {
        static unsafe void Main(string[] args)
        {

            byte[] buff = { 1, 2, 9, 7, 8, 6, 3, 4 };

            var x = new LittleEndianBytesIterator(buff);
            C.WriteLine($"{x.index}");
            C.WriteLine($"{(++x).index}");
            C.WriteLine($"{(x++).index}");
            C.WriteLine($"{(++x).index}");
            C.WriteLine($"{(++x).index}");
            C.WriteLine($"{(x++).index}");
            C.WriteLine($"{(++x).index}");
            C.WriteLine($"{x.index}");

            C.WriteLine();

            x = new LittleEndianBytesIterator(buff);
            C.WriteLine($"{x++.value}");
            C.WriteLine($"{x++.value}");
            C.WriteLine($"{x++.value}");
            C.WriteLine($"{x++.value}");
            C.WriteLine($"{x++.value}");
            C.WriteLine($"{x++.value}");
            C.WriteLine($"{x++.value}");
            C.WriteLine($"{x.value}");
            C.ReadKey();


            C.ReadKey();

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

        [StructLayout(LayoutKind.Explicit,
           Size = sizeof(ulong))]
        private struct UInt64Segment
        {
            [FieldOffset(0)]
            public ulong value;
            [FieldOffset(0)]
            public readonly byte byte0;
            [FieldOffset(1)]
            public readonly byte byte1;
            [FieldOffset(2)]
            public readonly byte byte2;
            [FieldOffset(3)]
            public readonly byte byte3;
            [FieldOffset(4)]
            public readonly byte byte4;
            [FieldOffset(5)]
            public readonly byte byte5;
            [FieldOffset(6)]
            public readonly byte byte6;
            [FieldOffset(7)]
            public readonly byte byte7;
        }

        private struct LittleEndianBytesIterator
        {
            public LittleEndianBytesIterator(byte[] source)
                : this(source,
                       new UInt64Segment { value = BitConverter.ToUInt64(source, 0) },
                       source[0],
                       0,
                       0)
            {
            }

            private LittleEndianBytesIterator(
                byte[] source,
                UInt64Segment segment,
                byte value,
                int index,
                int segmentByteIndex)
            {
                _source = source;
                _segment = segment;
                _segmentIndex = segmentByteIndex;
                this.value = value;
                this.index = index;
            }

            private readonly byte[] _source;
            private readonly UInt64Segment _segment;
            private readonly int _segmentIndex;

            public readonly byte value;
            public readonly int index;

            public static LittleEndianBytesIterator operator
                ++(LittleEndianBytesIterator x)
            {
                var i = x.index + 1;
                var j = x._segmentIndex + 1;
                var segment = x._segment;

                if (j == sizeof(ulong))
                {
                    j = 0;
                    segment = new UInt64Segment
                    {
                        value = BitConverter.ToUInt64(x._source, i)
                    };
                }

                byte value;
                switch (j)
                {
                    case 0: value = x._segment.byte0; break;
                    case 1: value = x._segment.byte1; break;
                    case 2: value = x._segment.byte2; break;
                    case 3: value = x._segment.byte3; break;
                    case 4: value = x._segment.byte4; break;
                    case 5: value = x._segment.byte5; break;
                    case 6: value = x._segment.byte6; break;
                    case 7: value = x._segment.byte7; break;
                    default: value = 0; break;
                }

                return new LittleEndianBytesIterator(
                    x._source, segment, value, i, j);
            }
        }
    }
}
