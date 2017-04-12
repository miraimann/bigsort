using System;

namespace Bigsort.Contracts
{
    public interface ILinesReservation<out TSegment>
        : ILinesStorage<TSegment>
    {
        void Load(int capacity);

        bool TryReserveRange(int length, 
            out IUsingHandle<Range> rangeHandle);
    }
}
