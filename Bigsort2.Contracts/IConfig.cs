namespace Bigsort2.Contracts
{
    public interface IConfig
    {
        string PartsDirectory { get; }
         
        int BufferSize { get; }
    }
}
