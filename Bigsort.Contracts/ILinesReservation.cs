namespace Bigsort.Contracts
{
    public interface ILinesReservation
        : ILinesStorage
    {
        void Load(int capacity);

        /// <summary>
        /// Tries to reserve and return IUsingHandle of range of lines (offset, length) 
        /// in lines reservation. Returns null if reservation out.
        /// </summary>
        IUsingHandle<Range> TryReserveRange(int length);
    }

    public interface ILinesReservation<out TSegment>
        : ILinesStorage<TSegment>
        , ILinesReservation
    {
    }
}
