namespace Bigsort.Contracts
{
    public interface ILinesReservation<out TSegment>
        : ILinesStorage<TSegment>
    {
        IDisposableValue<Range> TryReserveRange(int length);

        void Load();
    }
}
