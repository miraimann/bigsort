namespace Bigsort.Contracts
{
    // ReSharper disable once InconsistentNaming
    public interface IGrouperIOMaker
    {
        IGrouperIO Make(string input, string output);
        IGrouperIO[] MakeMany(string input, string output, int count);
    }
}
