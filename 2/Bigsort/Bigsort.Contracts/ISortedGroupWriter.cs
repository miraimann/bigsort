﻿namespace Bigsort.Contracts
{
    public interface ISortedGroupWriter
    {
        void Write(IGroupBytesMatrix group, 
                   Range linesRange, 
                   IWriter output);
    }
}
