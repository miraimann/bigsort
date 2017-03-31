using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GroupInfo
        : IGroupInfo
    {
        public GroupInfo(
            string name, 
            )

        public string Name { get; }
        public int ContentRowsCount { get; }
        public int ContentRowLength { get; }
        public int LinesCount { get; }
        public int BytesCount { get; }
    }
}
