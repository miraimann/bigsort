using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bigsort.Contracts;
using Bigsort.Implementation;
using Bigsort.Tools.TestFileGenerator;
using Moq;
using NUnit.Framework;
using static Bigsort.Tests.Tools;

namespace Bigsort.Tests
{
    public partial class GrouperTests
    {
        public class Integration
        {
            private const string UseExistanceFile = "Use existance file:";

            [TestCase("12_Kb [1-32].[0-128] E:\\12Kb", // inputFileSettings 
                      "E:\\12Kbgroups",               // groupsFile
                      180,                           // bufferSize  
                      2,                            // enginesCount
                      4,                           // maxThreadsCount
                      false                       // clear
                , Ignore = "for hands run only"
            )]

            [TestCase("3_Kb [1-32].[0-128] E:\\3Kb", // inputFileSettings 
                      "E:\\3Kbgroups",              // groupsFile
                      180,                         // bufferSize  
                      2,                          // enginesCount
                      4,                         // maxThreadsCount
                      true                      // clear
                , Ignore = "for hands run only"
            )]

            [TestCase("1_Kb [1-32].[0-128] E:\\1Kb", // inputFileSettings 
                      "E:\\1Kbgroups",              // groupsFile
                      180,                         // bufferSize  
                      2,                          // enginesCount
                      4,                         // maxThreadsCount
                      true                      // clear
                , Ignore = "for hands run only"
            )]

            [TestCase("1_Kb [1-32].[0] E:\\1Kb", // inputFileSettings 
                      "E:\\1Kbgroups",          // groupsFile
                      180,                     // bufferSize  
                      2,                      // enginesCount
                      4,                     // maxThreadsCount
                      true                  // clear
                , Ignore = "for hands run only"
            )]

            [TestCase(UseExistanceFile + " E:\\100Kb", // inputFileSettings 
                      "E:\\100Kbgroups",              // groupsFile
                      180,                           // bufferSize  
                      2,                            // enginesCount
                      4,                           // maxThreadsCount
                      true                        // clear
                , Ignore = "for hands run only"
            )]

            [TestCase("100_Mb [1-32].[0-128] E:\\100Mb", // inputFileSettings 
                      "E:\\100Mbgroups",                // groupsFile
                      180,                             // bufferSize  
                      2,                              // enginesCount
                      4,                             // maxThreadsCount
                      true                          // clear
                , Ignore = "for hands run only"
            )]

            [TestCase(UseExistanceFile + " E:\\100Mb", // inputFileSettings 
                      "E:\\100Mbgroups",              // groupsFile
                      180,                           // bufferSize  
                      2,                            // enginesCount
                      4,                           // maxThreadsCount
                      true                        // clear
                , Ignore = "for hands run only"
            )]

            public void Test(
                string inputFileSettings,
                string groupsFile,
                int bufferSize,
                int enginesCount,
                int maxThreadsCount,
                bool clear)
            {
                string inputFilePath = null;

                try
                {
                    if (inputFileSettings.StartsWith(UseExistanceFile))
                        inputFilePath = SplitString(inputFileSettings, ": ")[1];
                    else
                    {
                        var fileGenerationSettings = SplitString(inputFileSettings, " ");
                        Generator.Generate(sizeData: fileGenerationSettings[0],
                                       lineSettings: fileGenerationSettings[1],
                                               path: fileGenerationSettings[2]);
                        inputFilePath = fileGenerationSettings[2];
                    }

                    var configMock = new Mock<IConfig>();
                    var physicalBufferLength = bufferSize + 1;

                    configMock
                        .SetupGet(o => o.PhysicalBufferLength)
                        .Returns(physicalBufferLength);
                    
                    configMock
                        .SetupGet(o => o.UsingBufferLength)
                        .Returns(bufferSize);

                    configMock
                        .SetupGet(o => o.MaxRunningTasksCount)
                        .Returns(maxThreadsCount);

                    configMock
                        .SetupGet(o => o.GrouperEnginesCount)
                        .Returns(enginesCount);

                    configMock
                        .SetupGet(o => o.InputFilePath)
                        .Returns(inputFilePath);

                    configMock
                        .SetupGet(o => o.GroupsFilePath)
                        .Returns(groupsFile);

                    IGroupsInfoMarger groupsSummaryInfoMarger =
                        new GroupsInfoMarger();

                    ITasksQueue tasksQueue =
                        new TasksQueue(configMock.Object);

                    IBuffersPool buffersPool =
                        new InfinityBuffersPool(physicalBufferLength);

                    IIoService ioService =
                        new IoService(
                            buffersPool);

                    IPoolMaker poolMaker = 
                        new PoolMaker();

                    IInputReaderMaker inputReaderMaker =
                        new InputReaderMaker(
                            ioService,
                            tasksQueue,
                            buffersPool,
                            configMock.Object);

                    IGroupsLinesWriterFactory linesWriterFactory =
                        new GroupsLinesWriterFactory(
                            ioService,
                            tasksQueue,
                            poolMaker,
                            buffersPool,
                            configMock.Object);

                    IGrouperIOs grouperIOs =
                        new GrouperIOs(
                            inputReaderMaker,
                            linesWriterFactory,
                            ioService,
                            configMock.Object);
                    
                    ILinesIndexesExtractor linesIndexesExtractor =
                        new LinesIndexesExtractor(
                            configMock.Object);

                    IGroupsLoaderMaker groupsLoaderMaker =
                        new GroupsLoaderMaker(
                            buffersPool,
                            ioService,
                            configMock.Object);
                    
                    var grouper = new Grouper(
                        groupsSummaryInfoMarger,
                        grouperIOs,
                        tasksQueue,
                        configMock.Object);

                    var trivialGrouper = new TrivialGrouper();
                    var expectedGroups = trivialGrouper.SplitToGroups(
                        ReadAllLinesFrom(inputFilePath));

                    var groupsInfo = grouper.SplitToGroups();

                    var output = new IGroup[Consts.MaxGroupsCount];
                    var loader = groupsLoaderMaker.Make(groupsInfo, output);
                    loader.LoadNextGroups();
                    
                    var expectedGroupIds = expectedGroups
                        .Select(o => o.Id)
                        .ToArray();

                    var actualGroupIds = groupsInfo
                        .Select((group, id) => new { group, id })
                        .Where(o => !GroupInfo.IsZero(o.group))
                        .Select(o => o.id)
                        .ToArray();
#region DEBUG
// #if DEBUG
//                     var expectedGroupPrefixes = expectedGroupIds
//                         .Select(ToPrefix)
//                         .ToArray();
// 
//                     var actualGroupPrefixes = actualGroupIds
//                         .Select(ToPrefix)
//                         .ToArray();
// 
//                     var expectedGroupPrefixesInLine =
//                         string.Join(" | ", expectedGroupPrefixes);
// 
//                     var actualGroupPrefixesInLine = 
//                         string.Join(" | ", actualGroupPrefixes);
//                     
//                     var actualIdsOnly = actualGroupIds
//                         .Except(expectedGroupIds)
//                         .Select(ToPrefix);
//                     
//                     var actualIdsOnlyInLine =
//                         string.Join(" | ", actualIdsOnly);
//                     
//                     var expectedIdsOnly = expectedGroupIds
//                         .Except(actualGroupIds)
//                         .Select(ToPrefix);
//                     
//                     var allPrefixes =
//                         new[]
//                         {
//                             new[] {string.Empty},
//                     
//                             Enumerable.Range(' ', '~' - ' ' + 1)
//                                       .Select(o => ((char) o).ToString()),
//                     
//                             Enumerable.Join(
//                                 Enumerable.Range(' ', '~' - ' ' + 1),
//                                 Enumerable.Range(' ', '~' - ' ' + 1),
//                                 _ => true,
//                                 _ => true,
//                                 (c1, c2) => new string(new []{(char)c1, (char)c2}))
//                         }
//                         .Aggregate(Enumerable.Concat)
//                         .ToArray();
//                     
//                     var allCalculatedIds = allPrefixes
//                         .Select(ToId)
//                         .OrderBy(o => o)
//                         .ToArray();
//                     
//                     var allCalculatedIdsDistinct =
//                         allCalculatedIds.Distinct().ToArray();
//                     
//                     var allCalculatedPrefixes = Enumerable
//                         .Range(0, Consts.MaxGroupsCount)
//                         .Select(ToPrefix)
//                         .ToArray();
//                     
//                     var allCalculatedPrefixesDistinct = 
//                         allCalculatedPrefixes.Distinct().ToArray();
// #endif
#endregion
                    CollectionAssert.AreEqual(
                        expectedGroupIds,
                        actualGroupIds);
                    
                    int j = 0;
                    for (int i = 0; i < Consts.MaxGroupsCount; i++)
                    {
                        var info = groupsInfo[i];
                        if (GroupInfo.IsZero(info))
                            continue;

                        var expectedInfo = expectedGroups[j];
                        Assert.AreEqual(expectedInfo.BytesCount, info.BytesCount);
                        Assert.AreEqual(expectedInfo.LinesCount, info.LinesCount);
                        
                        linesIndexesExtractor.ExtractIndexes(output[i]);

                        var expectedLines = expectedInfo.Lines
                            .Select(o => o.Content)
                            .ToArray();

                        foreach (var line in expectedLines)
                            line[0] = Consts.EndLineByte1;

                        var expectedLinesDictionary = 
                            new Dictionary<HashedBytesArray, int>(info.LinesCount);

                        for (int k = 0; k < info.LinesCount; k++)
                        {
                            var hashedLine = Hash(expectedLines[k]);
                            if (expectedLinesDictionary.ContainsKey(hashedLine))
                                ++expectedLinesDictionary[hashedLine];
                            else expectedLinesDictionary.Add(hashedLine, 1);
                        }
#region DEBUG
// #if DEBUG
//                         var linesCountInDictionary = expectedLinesDictionary
//                             .Values.Sum(o => o);
// #endif
#endregion
                        var lines = output[i].Lines;
                        for (int k = 0; k < info.LinesCount; k++)
                        {
                            var lineIndexes = lines.Array[lines.Offset + k];
                            var lineLength = lineIndexes.LettersCount
                                           + lineIndexes.DigitsCount
                                           + 3;

                            var buffers = output[i].Buffers;
                            var bufferIndex = lineIndexes.Start / bufferSize;
                            var indexInBuffer = lineIndexes.Start % bufferSize;
                            var line = new byte[lineLength];
                            
                            if (indexInBuffer + lineLength <= bufferSize)
                                Array.Copy(buffers.Array[buffers.Offset + bufferIndex], indexInBuffer,
                                           line, 0,
                                           lineLength);
                            else
                            {
                                var bufferRightLength = bufferSize - indexInBuffer;
                                Array.Copy(buffers.Array[buffers.Offset + bufferIndex], indexInBuffer,
                                           line, 0,
                                           bufferRightLength);

                                Array.Copy(buffers.Array[buffers.Offset + bufferIndex + 1], 0,
                                           line, bufferRightLength,
                                           lineLength - bufferRightLength);
                            }

                            var actualHashedLine = Hash(line);
                            Assert.IsTrue(expectedLinesDictionary.ContainsKey(actualHashedLine));
                            --expectedLinesDictionary[actualHashedLine];
                            if (expectedLinesDictionary[actualHashedLine] == 0)
                                expectedLinesDictionary.Remove(actualHashedLine);
                        }

                        Assert.AreEqual(0, expectedLinesDictionary.Count);
                        ++j;
                    }

                    Assert.AreEqual(expectedGroups.Length, j);
                    loader.Dispose();
                }
                finally
                {
                    if (clear)
                    {
                        if (!inputFileSettings.StartsWith(UseExistanceFile)
                            && inputFilePath != null
                            && File.Exists(inputFilePath))
                            File.Delete(inputFilePath);

                        if (File.Exists(groupsFile))
                            File.Delete(groupsFile);
                    }
                }
            }
        }
    }
}
