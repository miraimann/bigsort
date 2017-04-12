using System.Collections.Generic;
using System.Linq;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GroupInfoMonoid
        : IGroupInfoMonoid
    {
        public IGroupInfo Null { get; } = null;

        public IGroupInfo Append(IGroupInfo x, IGroupInfo y) =>
            x == Null ? y : y == Null ? x : 
            new GroupInfo
            {
                LinesCount = x.LinesCount + y.LinesCount,
                BytesCount = x.BytesCount + y.BytesCount,
                Mapping = x.Mapping.Concat(y.Mapping)
            };

        private class GroupInfo
            : IGroupInfo
        {
            public IEnumerable<LongRange> Mapping { get; set; }
            public int LinesCount { get; set; }
            public int BytesCount { get; set; }
        }
    }
}
