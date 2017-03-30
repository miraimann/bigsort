using System.Collections.Generic;

namespace Bigsort.Contracts
{
    public interface IIoService
    {
        string TempDirectory { get; }

        string CurrentDirectory { get; set; }
        
        IReader OpenRead(string path);

        IWriter OpenWrite(string path);

        IEnumerable<string> EnumerateFilesOf(string directory);

        void CreateDirectory(string path);

        void DeleteFile(string path);
    }
}
