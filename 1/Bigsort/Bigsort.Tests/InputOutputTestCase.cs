using System.Linq;

namespace Bigsort.Tests
{
    public class InputOutputTestCase
    {
        public byte[] InputFileContent { get; private set; }
        public byte[] ExpectedOutputFileContent { get; private set; }

        public static InputOutputTestCase Parse(Seed seed) =>
            new InputOutputTestCase
            {
                InputFileContent = BytesOf(seed.InputFileContent),
                ExpectedOutputFileContent = BytesOf(seed.ExpectedOutputFileContent, true)
            };

        public static byte[] BytesOf(string[] lines, bool ended = false) =>
            string.Join(Consts.EndLine, lines)
                  .Select(o => (byte) o) //.Cast<byte>()
                  .Concat(ended && lines.Any() 
                             ? Consts.EndLineBytes 
                             : Enumerable.Empty<byte>())
                  .ToArray();

        public class Seed
        {
            private readonly string _name; 
            public Seed(string name)
            {
                _name = name;
            }

            public Seed() 
                : this("-")
            {
            }

            public string[] InputFileContent { get; set; }
            public string[] ExpectedOutputFileContent { get; set; }

            public override string ToString() =>
                _name;
        }
    }
}
