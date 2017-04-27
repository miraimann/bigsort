using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation.DevelopmentTools
{
    public class TimeTracker
        : ITimeTracker
    {
        private readonly ConcurrentDictionary<string, TimeSpan> _storage = 
            new ConcurrentDictionary<string, TimeSpan>();

        public void Add(string key, TimeSpan time) =>
            _storage.AddOrUpdate(key,
                 addValueFactory: _ => time,
              updateValueFactory: (_, acc) => acc + time);

        public IEnumerable<KeyValuePair<string, TimeSpan>> All =>
            _storage;

        public TimeSpan this[string key] =>
            _storage[key];
    }
}
