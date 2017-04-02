﻿using System.Collections.Generic;

namespace Bigsort.Contracts
{
    public interface IGrouper
    {
        /// <summary>
        /// Split file to group files by first 2 chracters of string part of lines.
        /// File has folloving bytes mapping in lines of result:
        /// [number length][letters length][number].[string]
        /// </summary>
        /// <param name="filePath">
        /// Source file path.
        /// </param>
        /// <param name="outputDirectory">
        /// Directory for output files, function uses current when it is null. 
        /// </param>
        /// <returns>
        /// Returns info of all created groups ordered by names.
        /// </returns>
        IEnumerable<IGroupInfo> SplitToGroups(
            string filePath,
            string outputDirectory = null);
    }
}
