using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    internal class GroupsInfoMarger
        : IGroupsInfoMarger
    {
        public GroupInfo[] Marge(GroupInfo[][] summaryInfos)
        {
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

            return summaryInfos[0];
        }
    }
}
