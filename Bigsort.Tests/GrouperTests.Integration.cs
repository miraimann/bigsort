﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            private const int GroupBufferRowReadingEnsurance = 7;

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

                    configMock
                        .SetupGet(o => o.BufferSize)
                        .Returns(bufferSize);

                    configMock
                        .SetupGet(o => o.MaxRunningTasksCount)
                        .Returns(maxThreadsCount);

                    configMock
                        .SetupGet(o => o.GrouperEnginesCount)
                        .Returns(enginesCount);

                    configMock
                        .SetupGet(o => o.GroupBufferRowReadingEnsurance)
                        .Returns(GroupBufferRowReadingEnsurance);

                    IGroupInfoMonoid groupInfoMonoid =
                        new GroupInfoMonoid();

                    IGroupsSummaryInfoMarger groupsSummaryInfoMarger =
                        new GroupsSummaryInfoMarger(groupInfoMonoid);

                    IUsingHandleMaker usingHandleMaker =
                        new UsingHandleMaker();

                    IPoolMaker poolMaker =
                        new PoolMaker(usingHandleMaker);

                    ITasksQueue tasksQueue =
                        new TasksQueue(configMock.Object);

                    IBuffersPool buffersPool =
                        new BuffersPool(poolMaker, configMock.Object);

                    IIoService ioService =
                        new IoService(
                            buffersPool);

                    IGrouperBuffersProviderMaker grouperBuffersProviderMaker =
                        new GrouperBuffersProviderMaker(
                            buffersPool,
                            ioService,
                            usingHandleMaker,
                            tasksQueue,
                            configMock.Object);

                    IGroupsLinesWriterMaker linesWriterMaker =
                        new GroupsLinesWriterMaker(
                            ioService,
                            buffersPool,
                            tasksQueue,
                            configMock.Object);

                    IGrouperIOMaker grouperIoMaker =
                        new GrouperIOMaker(
                            grouperBuffersProviderMaker,
                            linesWriterMaker,
                            ioService,
                            configMock.Object);

                    IGroupBytesMatrixService groupBytesMatrixService =
                        new GroupBytesMatrixService(
                            buffersPool,
                            configMock.Object);

                    var linesIndexesStorageMock = new Mock<ILinesIndexesStorage>();

                    ILinesIndexesExtractor linesIndexesExtractor =
                        new LinesIndexesExtractor(
                            linesIndexesStorageMock.Object);

                    var grouper = new Grouper(
                        groupsSummaryInfoMarger,
                        grouperIoMaker,
                        tasksQueue,
                        configMock.Object);

                    var trivialGrouper = new TrivialGrouper();
                    var expectedGroups = trivialGrouper.SplitToGroups(
                        ReadAllLinesFrom(inputFilePath));

                    var summary = grouper
                        .SplitToGroups(inputFilePath, groupsFile);

                    var expectedGroupIds = expectedGroups
                        .Select(o => o.Id)
                        .ToArray();

                    var actualGroupIds = summary.GroupsInfo
                        .Select((group, id) => new {group, id})
                        .Where(o => o.group != null)
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

                    Assert.AreEqual(
                        expectedGroups.Max(group => group.LinesCount),
                        summary.MaxGroupLinesCount);

                    Assert.AreEqual(
                        expectedGroups.Max(group => group.BytesCount),
                        summary.MaxGroupSize);

                    int j = 0;
                    for (int i = 0; i < Consts.MaxGroupsCount; i++)
                    {
                        if (summary.GroupsInfo[i] == null)
                            continue;

                        Assert.AreEqual(
                            expectedGroups[j].BytesCount,
                            summary.GroupsInfo[i].BytesCount);

                        Assert.AreEqual(
                            expectedGroups[j].LinesCount,
                            summary.GroupsInfo[i].LinesCount);

                        var rowInfo = groupBytesMatrixService
                            .CalculateRowsInfo(summary.GroupsInfo[i].BytesCount);

                        IGroupBytesMatrix matrix;
                        using (var reader = ioService.OpenRead(groupsFile))
                            matrix = groupBytesMatrixService
                                .LoadMatrix(rowInfo, summary.GroupsInfo[i], reader);

                        var lineIndexes = new LineIndexes[summary.GroupsInfo[i].LinesCount];

                        linesIndexesStorageMock
                            .Setup(o => o.Indexes)
                            .Returns(lineIndexes);

                        linesIndexesExtractor.ExtractIndexes(matrix,
                            new Contracts.Range(0, summary.GroupsInfo[i].LinesCount));

                        var expectedLines = expectedGroups[j].Lines
                            .Select(o => o.Content)
                            .ToArray();

                        foreach (var line in expectedLines)
                            line[0] = Consts.EndLineByte1;

                        var expectedLinesDictionary = new Dictionary<HashedBytesArray, int>(
                            summary.GroupsInfo[i].LinesCount);

                        for (int k = 0; k < summary.GroupsInfo[i].LinesCount; k++)
                        {
                            var hashedLine = Hash(expectedLines[k]);
                            if (expectedLinesDictionary.ContainsKey(hashedLine))
                                ++ expectedLinesDictionary[hashedLine];
                            else expectedLinesDictionary.Add(hashedLine, 1);
                        }
#region DEBUG
// #if DEBUG
//                         var linesCountInDictionary = expectedLinesDictionary
//                             .Values.Sum(o => o);
// #endif
#endregion
                        for (int k = 0; k < summary.GroupsInfo[i].LinesCount; k++)
                        {
                            var lineLength = lineIndexes[k].lettersCount
                                           + lineIndexes[k].digitsCount
                                           + 3;

                            var line = new byte[lineLength];
                            for (int m = 0; m < lineLength; m++)
                                line[m] = matrix[lineIndexes[k].start + m];

                            var actualHashedLine = Hash(line);
                            Assert.IsTrue(expectedLinesDictionary.ContainsKey(actualHashedLine));
                            -- expectedLinesDictionary[actualHashedLine];
                            if (expectedLinesDictionary[actualHashedLine] == 0)
                                expectedLinesDictionary.Remove(actualHashedLine);
                        }

                        Assert.AreEqual(0, expectedLinesDictionary.Count);
                        ++j;
                    }

                    Assert.AreEqual(expectedGroups.Length, j);
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