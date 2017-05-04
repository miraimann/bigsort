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

        void DeleteFile(string path);
    }
}
