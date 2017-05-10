namespace Bigsort.Contracts
{
    public interface IGroupsInfoMarger
    {
        /// <summary>
        /// Marges summaryInfos. Corrupts argument value.
        /// </summary>
        GroupInfo[] Marge(GroupInfo[][] summaryInfos);
    }
}
