using System;
using System.Configuration;
using System.IO;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class Config
        : IConfig
    {
        public Config()
        {
            string raw;

            GroupsFilePath = Path.GetTempFileName();

            SortingSegment = ConfigurationManager
                .AppSettings["BigsortSortingSegment"]
                ?? "ulong"; // "byte", "uint"

            raw = ConfigurationManager
                .AppSettings["BigsortMaxRunningTasksCount"];

            MaxRunningTasksCount = 3; //raw == null
                // ? Environment.ProcessorCount - 1
                // : int.Parse(raw);

            raw = ConfigurationManager
                .AppSettings["BigsortGrouperEnginesCount"];

            GrouperEnginesCount = 1; //Math.Min(3, raw == null
                // ? Environment.ProcessorCount / 2
                // : int.Parse(raw));

            GroupBufferRowReadingEnsurance =
                ( SortingSegment == "ulong" ? sizeof(ulong)
                : SortingSegment == "uint"  ? sizeof(uint)
                : SortingSegment == "uint"  ? sizeof(byte)
                : 0 /* invalid value */) 
                - 1;

            raw = ConfigurationManager
                .AppSettings["BigsortBufferSize"];

            BufferSize = raw != null
                ? int.Parse(raw)
                : 256 * 1024;

            raw = ConfigurationManager
                .AppSettings["BigsortMaxMemoryForLines"];
            
            MaxMemoryForLines = raw != null
                ? long.Parse(raw)
                : 320 * 1024 * 1024;

            GroupRowLength = BufferSize;
            // - GroupBufferRowReadingEnsurance;
        }

        public string GroupsFilePath { get; }
        public string SortingSegment { get; }
        public int BufferSize { get; }
        public long MaxMemoryForLines { get; }
        public int MaxRunningTasksCount { get; }
        public int GroupRowLength { get; }
        public int GrouperEnginesCount { get; }
        public int GroupBufferRowReadingEnsurance { get; }
    }
}
