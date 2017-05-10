using System.Collections.Generic;

namespace Bigsort.Contracts
{
    internal struct GroupInfo
    {
        /// <summary>
        /// Mapping is ranges (offset, count) of group bytes blocks 
        /// in grouping result file.
        /// </summary>
        public IReadOnlyList<LongRange> Mapping;
        public int LinesCount;
        public int BytesCount;

        public static bool IsZero(GroupInfo info) =>
            info.LinesCount == 0;
    }
}
