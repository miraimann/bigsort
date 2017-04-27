namespace Bigsort.Contracts
{
    public interface IGroupsService
    {
        /// <summary>
        /// Tries to create and return group. 
        /// Returns null if memory resources out for creation.
        /// </summary>
        IGroup TryCreateGroup(GroupInfo groupInfo);

        void LoadGroup(
            IGroup group, 
            GroupInfo groupInfo, 
            IFileReader groupsFileReader);

        IGroupsLoader MakeGroupsLoader(
            string groupFilePath, 
            IGroupsSummaryInfo groupsSummary,
            IGroup[] output);
    }
}
