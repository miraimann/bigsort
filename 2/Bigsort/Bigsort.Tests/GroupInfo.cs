using Bigsort.Contracts;

namespace Bigsort.Tests
{
    public class GroupInfo
        : IGroupInfo
    {
        public GroupInfo(
            string name,
            int linesCount,
            int bytesCount)
        {
            Name = name;
            LinesCount = linesCount;
            BytesCount = bytesCount;
        }

        public string Name { get; }
        public int LinesCount { get; }
        public int BytesCount { get; }
    }
}
