using System;
using Bigsort.Contracts;
using Bigsort.Contracts.DevelopmentTools;

namespace Bigsort.Implementation
{
    public class GroupsSummaryInfoMarger
        : IGroupsInfoMarger
    {
        public const string
            LogName = nameof(GroupsSummaryInfoMarger),
            MargingLogName = LogName + "." + nameof(Marge);

        private readonly ITimeTracker _timeTracker;

        public GroupsSummaryInfoMarger(
            IDiagnosticTools diagnosticTools = null)
        {
            _timeTracker = diagnosticTools?.TimeTracker;
        }

        public GroupInfo[] Marge(GroupInfo[][] summaryInfos)
        {
            var start = DateTime.Now;
            
            for (int i = 0; i < Consts.MaxGroupsCount; i++)
            {
                int j = 0;
                var acc = default(GroupInfo);
                while (j < summaryInfos.Length 
                    && GroupInfo.IsZero(acc = summaryInfos[j++][i]))
                    ;

                var mappingBlocksCount = 0;
                while (j < summaryInfos.Length)
                {
                    var info = summaryInfos[j++][i];
                    if (!GroupInfo.IsZero(info))
                    {
                        mappingBlocksCount += info.Mapping.Count;
                        acc.BytesCount += info.BytesCount;
                        acc.LinesCount += info.LinesCount;
                    }   
                }

                if (!GroupInfo.IsZero(acc))
                {
                    if (mappingBlocksCount != 0)
                    {
                        int k = 0;
                        var mapping = new LongRange[mappingBlocksCount + acc.Mapping.Count];
                        for (j = 0; j < summaryInfos.Length; j++)
                        {
                            var infoMapping = summaryInfos[j][i].Mapping;
                            if (infoMapping != null)
                                for (int h = 0; h < infoMapping.Count; h++, k++)
                                    mapping[k] = infoMapping[h];
                        }

                        acc.Mapping = mapping;
                    }
                    
                    summaryInfos[0][i] = acc;
                }
            }
            
            _timeTracker?.Add(MargingLogName, DateTime.Now - start);

            return summaryInfos[0];
        }
    }
}
