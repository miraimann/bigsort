namespace Bigsort.Contracts.DevelopmentTools
{
    internal interface IDiagnosticTools
    {
        ITimeTracker TimeTracker { get; }

        ILogRoot Log { get; }

        ILogger GetLogger(string key);
    }
}
