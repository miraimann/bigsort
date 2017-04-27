namespace Bigsort.Contracts.DevelopmentTools
{
    public interface IDiagnosticTools
    {
        ITimeTracker TimeTracker { get; }

        ILogRoot Log { get; }

        ILogger GetLogger(string key);
    }
}
