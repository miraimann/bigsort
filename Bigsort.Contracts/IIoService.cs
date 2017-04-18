namespace Bigsort.Contracts
{
    public interface IIoService
    {
        
        IFileReader OpenRead(string path, 
            long position = 0);

        IFileWriter OpenWrite(string path, 
            long position = 0, bool buffering = false);
        
        long SizeOfFile(string path);

        void CreateFile(string path, long length);

        string CreateTempFile(long length);

        bool DirectoryExists(string path);

        void CreateDirectory(string path);

        bool FileExists(string path);

        void DeleteFile(string path);
    }
}
