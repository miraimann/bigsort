using System.Collections.Generic;

namespace Bigsort.Contracts
{
    public interface IGroupsSummaryInfoMarger
    {
        /// <summary>
        /// Marge summaryInfos. Corrupt argument value.
        /// </summary>
        IGroupsSummaryInfo Marge(GroupInfo[][] summaryInfos);
    }
}
