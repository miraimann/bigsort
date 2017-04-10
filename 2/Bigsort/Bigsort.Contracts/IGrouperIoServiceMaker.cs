namespace Bigsort.Contracts
{
    public interface IGrouperIoServiceMaker
    {
        IGrouperIoService Make(
            string input, string output);

        IGrouperIoService[] MakeMany(
            string input, string output, int count);
    }
}
