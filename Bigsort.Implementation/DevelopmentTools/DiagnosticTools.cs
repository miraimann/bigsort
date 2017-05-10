using System;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation.DevelopmentTools
{
    internal class DiagnosticTools
        : IDiagnosticTools
    {
        public DiagnosticTools(ITimeTracker timeTracker)
        {
            TimeTracker = timeTracker;
        }

        public ITimeTracker TimeTracker { get; }

        public ILogRoot Log
        {
            get { throw new NotImplementedException(); }
        }

        public ILogger GetLogger(string key)
        {
            throw new NotImplementedException();
        }
    }
}
