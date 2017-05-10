using System.Threading;

namespace Bigsort.Contracts
{
    public interface IGroupsLinesWriter
    {
        void AddLine(ushort groupId, 
            byte[] buff, int offset, int length);

        void AddBrokenLine(ushort groupId,
            byte[] leftBuff, int leftOffset, int leftLength,
            byte[] rightBuff, int rightLength);

        void FlushAndDispose(ManualResetEvent done);
        
        GroupInfo[] SelectSummaryGroupsInfo();
    }
}
