using System.Linq;

namespace Bigsort.Tests
{
    public class Consts
    {
        public const string
            InputFilePath = "ZZZzzzZZzzZ",
            OutputFilePath = "WWwwwWWWwwWwWw";

        public const byte Dot = (byte) '.';

        public static readonly byte[] EndLineBytes = { (byte)'\n', (byte)'\r' };
        public static readonly string EndLine =
            new string(EndLineBytes.Select(o => (char)o).ToArray());
    }
}
