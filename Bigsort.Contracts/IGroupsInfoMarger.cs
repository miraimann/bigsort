namespace Bigsort.Contracts
{
    internal interface IGroupsInfoMarger
    {
        /// <summary>
        /// Marges summaryInfos. Corrupts argument value.
        /// </summary>
        GroupInfo[] Marge(GroupInfo[][] summaryInfos);
    }
}
