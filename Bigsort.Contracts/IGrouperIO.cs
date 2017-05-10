namespace Bigsort.Contracts
{
    internal interface IGrouperIO
    {
        IInputReader Input { get; }
        
        IGroupsLinesWriter Output { get; }
    }
}
