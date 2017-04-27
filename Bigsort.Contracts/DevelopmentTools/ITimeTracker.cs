using System;
using System.Collections.Generic;

namespace Bigsort.Contracts.DevelopmentTools
{
    public interface ITimeTracker
    {
        void Add(string key, TimeSpan time);
        
        IEnumerable<KeyValuePair<string, TimeSpan>> All { get; }

        TimeSpan this[string key] { get; }
    }
}
