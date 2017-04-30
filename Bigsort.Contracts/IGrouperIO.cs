namespace Bigsort.Contracts
{
    // ReSharper disable once InconsistentNaming
    public interface IGrouperIO
    {
        IInputReader Input { get; }
        
        IGroupsLinesWriter Output { get; }
    }
}
