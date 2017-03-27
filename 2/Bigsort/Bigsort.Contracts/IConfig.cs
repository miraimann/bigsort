namespace Bigsort.Contracts
{
    public interface IConfig
    {
        string PartsDirectory { get; }
         
        int BufferSize { get; }
    }
}
