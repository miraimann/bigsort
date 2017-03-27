using Bigsort.Contracts;
using System;
using System.Linq;

namespace Bigsort.Implementation
{
    internal class Config
        : IConfig
    {
        public Config()
        {
            MaxCollectionSize = 512 * 1024 * 1024;
            IntsAccumulatorFragmentSize = 64;
            ResultWriterBufferSize = 32 * 1024;
            BytesEnumeratingBufferSize = 32 * 1024;
            EndLine = Environment.NewLine
                .Select(o => (byte) o)
                .ToArray();

            Dot = (byte) '.';
        }

        public int MaxCollectionSize { get; }
        public int IntsAccumulatorFragmentSize { get; }
        public int BytesEnumeratingBufferSize { get; }
        public int ResultWriterBufferSize { get; }
        public byte[] EndLine { get; }
        public byte Dot { get; }
    }
}
