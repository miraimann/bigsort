using System.Threading;

namespace Bigsort.Contracts
{
    internal interface IGroupsLinesOutput
    {
        void ReleaseLine(ushort groupId, 
            byte[] buff, int offset, int length);

        void ReleaseBrokenLine(ushort groupId,
            byte[] leftBuff, int leftOffset, int leftLength,
            byte[] rightBuff, int rightLength);

        void FlushAndDispose(CountdownEvent done);
        
        GroupInfo[] SelectSummaryGroupsInfo();
    }
}
