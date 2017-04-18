using System;
using System.Collections.Generic;
using System.Linq;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GroupsSummaryInfoMarger
        : IGroupsSummaryInfoMarger
    {
        private readonly IGroupInfoMonoid _groupInfoMonoid;

        public GroupsSummaryInfoMarger(IGroupInfoMonoid groupInfoMonoid)
        {
            _groupInfoMonoid = groupInfoMonoid;
        }

        public IGroupsSummaryInfo Marge(IEnumerable<IEnumerable<IGroupInfo>> summaryInfos)
        {
            IGroupInfo[] margedSummaryInfos = summaryInfos
                .Aggregate(Enumerable.Repeat(_groupInfoMonoid.Null, Consts.MaxGroupsCount),
                          (acc, o) => Enumerable.Zip(acc, o, _groupInfoMonoid.Append))
                .ToArray();

            var maxOf = margedSummaryInfos
                .Aggregate(new {linesCount = 0, size = 0},
                    (acc, o) => o == null ? acc
                         : new
                         {
                             linesCount = Math.Max(acc.linesCount, o.LinesCount),
                             size = Math.Max(acc.size, o.BytesCount)
                         });

            return new Summary
            {
                GroupsInfo = margedSummaryInfos,
                MaxGroupLinesCount = maxOf.linesCount,
                MaxGroupSize = maxOf.size
            };
        }

        private class Summary
            : IGroupsSummaryInfo
        {
            public IGroupInfo[] GroupsInfo { get; internal set; }
            public int MaxGroupLinesCount { get; internal set; }
            public int MaxGroupSize { get; internal set; }
        }
    }
}
