namespace Bigsort.Contracts.DevelopmentTools
{
    internal interface ILogRoot
        : ILog
    {
        ILog this[params string[] keys] { get; } 
    }
}
