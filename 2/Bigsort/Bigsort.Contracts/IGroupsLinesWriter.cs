using System.Collections.Generic;
using System.Threading;

namespace Bigsort.Contracts
{
    public interface IGroupsLinesWriter
    {
        IReadOnlyList<IGroupInfo> SummaryGroupsInfo { get; }

        void AddLine(ushort groupId, 
            byte[] buff, int offset, int length);

        void AddBrokenLine(ushort groupId,
            byte[] leftBuff, int leftOffset, int leftLength,
            byte[] rightBuff, int rightOffset, int rightLength);

        void FlushAndDispose(ManualResetEvent done);
    }
}
