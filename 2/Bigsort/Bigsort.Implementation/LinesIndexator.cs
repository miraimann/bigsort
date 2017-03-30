using System.Collections.Generic;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class LinesIndexator
        : ILinesIndexator
    {
        public void IndexLines(
            IReadOnlyList<byte> group,
            ArrayFragment<SortingLine> lines)
        {
            var content = lines.Array;
            int offset = lines.Offset,
                last = offset + lines.Count,
                i = 4; // skip first 4 bytes with lines count

            content[offset].start = i;
            while (offset <= last)
            {
                i += group[++i] & 0xEF;
                i += group[++i];
                content[++offset].start = i;
            }
        }
    }
}
