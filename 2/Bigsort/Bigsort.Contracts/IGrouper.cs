using System.Collections.Generic;

namespace Bigsort.Contracts
{
    public interface IGrouper
    {
        /// <summary>
        /// Split file to group files by first 2 chracters of string part of lines.
        /// (Create files in Current directory.)
        /// </summary>
        /// <param name="filePath">
        /// Source file path.
        /// </param>
        /// <returns>
        /// Returns info of all created groups ordered by names.
        /// </returns>
        IEnumerable<IGroupInfo> SplitToGroups(string filePath);
    }
}
