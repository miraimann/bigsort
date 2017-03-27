namespace Bigsort2.Contracts
{
    public interface IIoService
    {
        string TempDirectory { get; }

        string CurrentDirectory { get; set; }
        
        IReader OpenRead(string path);

        IWriter OpenWrite(string path);

        void DeleteFile(string path);
    }
}
