namespace Bigsort.Contracts
{
    public interface IIoService
    {
        string TempDirectory { get; }

        string CurrentDirectory { get; set; }
        
        IReader OpenRead(string path);

        IBytesMatrix ReadToBytesMatrix(string path);

        IWriter OpenWrite(string path);

        void CreateDirectory(string path);

        void DeleteFile(string path);
    }
}
