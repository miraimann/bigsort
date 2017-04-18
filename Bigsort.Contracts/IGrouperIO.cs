namespace Bigsort.Contracts
{
    // ReSharper disable once InconsistentNaming
    public interface IGrouperIO
    {
        IGrouperBuffersProvider Input { get; }
        
        IGroupsLinesWriter Output { get; }
    }
}
