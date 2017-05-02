using System.Collections.Generic;

namespace Bigsort.Contracts
{
    public interface IGroupsInfoMarger
    {
        /// <summary>
        /// Marge summaryInfos. Corrupt argument value.
        /// </summary>
        GroupInfo[] Marge(GroupInfo[][] summaryInfos);
    }
}
