using System.Collections.Generic;

namespace Bigsort.Contracts
{
    public interface IGroupInfo
    {
        /// <summary>
        /// Gets ranges (offset, count) of group bytes blocks 
        /// of grouping result file.
        /// </summary>
        IEnumerable<LongRange> Mapping { get; }

        int LinesCount { get; }
        int BytesCount { get; }
    }
}
