using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Bigsort.Contracts;
using Microsoft.VisualBasic.Devices;

namespace Bigsort.Implementation
{
    public class Config
        : IConfig
    {
        public Config(
            string inputFilePath,
            string outputFilePath,
            string groupsFilePath)
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

            GrouperEnginesCount = raw == null 
                ? Math.Max(1, Environment.ProcessorCount / 4) 
                : int.Parse(raw);
       
            raw = ConfigurationManager
                .AppSettings["BigsortUsingBufferLength"];

            if (raw != null)
                UsingBufferLength = int.Parse(raw);
            else
            {
                UsingBufferLength = (int)
                    ((new ComputerInfo().AvailablePhysicalMemory * 0.8) /
                     (Consts.MaxGroupsCount * GrouperEnginesCount));

                var pathes = new[]
                {
                    inputFilePath,
                    outputFilePath,
                    groupsFilePath,
                };

                uint _, __, sectorsPerCluster, bytesPerSector;
                var bytesPerClusters = new uint[pathes.Length];
                for (int i = 0; i < pathes.Length;
                     bytesPerClusters[i++] = bytesPerSector * sectorsPerCluster)
                    GetDiskFreeSpace(
                        Path.GetPathRoot(pathes[i]),
                        out bytesPerSector,
                        out sectorsPerCluster,
                        out __,
                        out _
                    );

                Func<uint, uint, uint> lcm, gcd = null;
                gcd = (a, b) => b == 0 ? a : gcd(b, a % b); // Greatest common divisor 
                lcm = (a, b) => a / gcd(a, b) * b;          // Least common multiple

                var bytesPerClustersLcm = 
                    (int) bytesPerClusters.Aggregate(lcm);

                var startingBufferLength = PhysicalBufferLength;
                UsingBufferLength /= bytesPerClustersLcm;
                UsingBufferLength *= bytesPerClustersLcm;
                
                if (UsingBufferLength == 0)
                    for (UsingBufferLength = bytesPerClustersLcm;
                         UsingBufferLength > startingBufferLength;
                         UsingBufferLength /= 2
                        ) ;
            }

            PhysicalBufferLength = UsingBufferLength
                                 + Consts.BufferReadingEnsurance;
        }

        public string GroupsFileDirectoryPath { get; }
        public int PhysicalBufferLength { get; }
        public int UsingBufferLength { get; }
        public int MaxRunningTasksCount { get; }
        public int GrouperEnginesCount { get; }

        [DllImport("kernel32.dll",
            SetLastError = true,
            CharSet = CharSet.Auto)]
        static extern bool GetDiskFreeSpace(
            string rootPathName,
            out uint sectorsPerCluster,
            out uint bytesPerSector,
            out uint numberOfFreeClusters,
            out uint totalNumberOfFreeClusters);
    }
}
