using System;
using System.Threading.Tasks;

namespace Bigsort.Implementation
{
    internal class Consts
    {
        public const int
            AsciiPrintableCharsCount = 95,
            AsciiPrintableCharsOffset = 32,
            GroupIdLettersCount = 2,
            // all combination from 0 to 2 (GroupIdLettersCount) chars 
            // where 96 is ASCIIPrintableCharsCount
            MaxGroupsCount = AsciiPrintableCharsCount*AsciiPrintableCharsCount
                             + AsciiPrintableCharsCount + 1,
            TemporaryMissingResult = -1,
            EndLineBytesCount = 2,
            SegmentSize = sizeof(ulong),
            BufferReadingEnsurance = SegmentSize - 1,
            BitsInByteCount = 8;

        public const ulong
            SegmentDigitsOut = ulong.MaxValue,
            SegmentLettersOut = ulong.MinValue;

        public const byte
            Dot = (byte) '.',
            EndLineByte1 = (byte) '\r',
            EndLineByte2 = (byte) '\n';

        public static readonly byte[] EndLineBytes =
            {EndLineByte1, EndLineByte2};

        public static readonly Action ZeroAction = () => { };
        
        public static readonly int MaxRunningTasksCount =
            Math.Max(1, Environment.ProcessorCount - 1);

        public static readonly ParallelOptions UseMaxTasksCountOptions =
            new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxRunningTasksCount
            };
    }
}
