using System;

namespace Bigsort.Contracts
{
    public interface ILinesReservation<out TSegment>
        : ILinesStorage<TSegment>
    {
        void Load();

        IDisposableValue<Range> TryReserveRange(int length);
    }
}
