namespace Bigsort.Contracts
{
    public interface ILinesReservation
        : ILinesStorage
    {
        void Load(int capacity);

        bool TryReserveRange(int length,
            out IUsingHandle<Range> rangeHandle);
    }

    public interface ILinesReservation<out TSegment>
        : ILinesStorage<TSegment>
        , ILinesReservation
    {
    }
}
