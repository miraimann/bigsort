using System.Collections.Generic;
using System.IO;

namespace Bigsort.Contracts
{
    internal interface IIoService
    {
        string TempDirectory { get; }

        IEnumerable<byte> EnumeratesBytesOf(string path);
        
        IReadingStream OpenRead(string path);

        IWritingStream OpenWrite(string path);
        
        IStream CreateInMemory();

        IStream Adapt(MemoryStream stream);

        ITextWriter Adapt(TextWriter writer);

        void DeleteFile(string path);
    }
}
