using System;

namespace Bigsort.Contracts
{
    public interface ILinesReservation<out TSegment>
        : ILinesStorage<TSegment>
    {
        void Load();

        IUsingHandle<Range> TryReserveRange(int length);
    }
}
