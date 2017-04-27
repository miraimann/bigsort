namespace Bigsort.Contracts.DevelopmentTools
{
    public interface ILogRoot
        : ILog
    {
        ILog this[params string[] keys] { get; } 
    }
}
