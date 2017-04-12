using System.Collections.Generic;

namespace Bigsort.Contracts
{
    public interface IGroupsSummaryInfoMarger
    {
        IGroupsSummaryInfo Marge(
            IEnumerable<IEnumerable<IGroupInfo>> summaryInfos);
    }
}
