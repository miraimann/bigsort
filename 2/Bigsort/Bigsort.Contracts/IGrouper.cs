namespace Bigsort.Contracts
{
    public interface IGrouper
    {
        /// <summary>
        /// Split file to group files by first 2 chracters of string part of lines.
        /// </summary>
        /// <param name="filePath">
        /// Source file path.
        /// </param>
        /// <returns>
        /// Path to directory with result files.
        /// </returns>
        string SplitToGroups(string filePath);
    }
}
