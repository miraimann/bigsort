using System.Collections.Generic;

namespace Bigsort.Contracts
{
    public interface IIoService
    {
        string TempDirectory { get; }

        string CurrentDirectory { get; set; }
        
        IReader OpenRead(string path);

        IAsyncReader OpenAsyncRead(string path, ITasksQueue tasksQueue);

        IWriter OpenWrite(string path);

        IWriter OpenBufferingWrite(string path);
        
        IAsyncWriter OpenAsyncBufferingWrite(string path, ITasksQueue taskQueue);
        
        IWriter OpenBufferingAsyncWrite(string path, ITasksQueue taskQueue);

        IWriter OpenSharedWrite(string path, long possition);

        IEnumerable<string> EnumerateFilesOf(string directory);

        long SizeOfFile(string path);

        void CreateFile(string path, long length);

        bool DirectoryExists(string path);

        void CreateDirectory(string path);

        void DeleteFile(string path);
    }
}
