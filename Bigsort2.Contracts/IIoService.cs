using System.Collections.Generic;
using System.IO;

namespace Bigsort2.Contracts
{
    public interface IIoService
    {
        string TempDirectory { get; }

        void SetCurrentDirectory(string pat);
        
        IReader OpenRead(string path);

        IWriter OpenWrite(string path);
        
        //IStream CreateInMemory();

        //IStream Adapt(MemoryStream stream);

        //ITextWriter Adapt(TextWriter writer);

        void DeleteFile(string path);
    }
}
