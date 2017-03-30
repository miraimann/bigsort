namespace Bigsort.Contracts
{
    public interface IBitReaderMaker
    {
        IBitReader MakeFor(IBytesMatrix group);
    }
}
