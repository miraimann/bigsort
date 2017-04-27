namespace Bigsort.Contracts.DevelopmentTools
{
    public interface ILogger
    {
        ILogger Write(string msg);
        ILogger WriteLine(string msg);
        ILogger WriteLine();
    }
}
