using System;
using System.Collections.Generic;

namespace Bigsort.Contracts.DevelopmentTools
{
    internal interface ITimeTracker
    {
        void Add(string key, TimeSpan time);
        
        IEnumerable<KeyValuePair<string, TimeSpan>> All { get; }

        TimeSpan this[string key] { get; }
    }
}
