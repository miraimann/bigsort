namespace Bigsort.Contracts.DevelopmentTools
{
    internal interface ILogger
    {
        ILogger Write(string msg);
        ILogger WriteLine(string msg);
        ILogger WriteLine();
    }
}
