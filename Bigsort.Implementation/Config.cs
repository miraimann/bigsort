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

            GroupsFileDirectoryPath = Path.GetTempPath();

            raw = ConfigurationManager
                .AppSettings["BigsortMaxRunningTasksCount"];

            MaxRunningTasksCount = raw == null
                ? Consts.MaxRunningTasksCount
                : int.Parse(raw);

            raw = ConfigurationManager
                .AppSettings["BigsortGrouperEnginesCount"];

            GrouperEnginesCount = raw == null ? 1 : int.Parse(raw);

            raw = ConfigurationManager
                .AppSettings["BigsortPhysicalBufferLength"];

            PhysicalBufferLength = raw != null
                ? int.Parse(raw)
                : 256 * 1024;

            UsingBufferLength = PhysicalBufferLength 
                              - Consts.BufferReadingEnsurance;
        }

        public string GroupsFileDirectoryPath { get; }
        public int PhysicalBufferLength { get; }
        public int UsingBufferLength { get; }
        public int MaxRunningTasksCount { get; }
        public int GrouperEnginesCount { get; }
    }
}
