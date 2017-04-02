using System.Collections.Generic;

namespace Bigsort.Contracts
{
    public interface IIoService
    {
        string TempDirectory { get; }

        string CurrentDirectory { get; set; }
        
        IReader OpenRead(string path);

        IWriter OpenWrite(string path);

        IWriter OpenSharedWrite(string path, long possition);

        IEnumerable<string> EnumerateFilesOf(string directory);

        long SizeOfFile(string path);

        void CreateFile(string path, long length);

        bool DirectoryExists(string path);

        void CreateDirectory(string path);

        void DeleteFile(string path);
    }
}
