namespace Bigsort.Contracts
{
    internal interface IGrouperIO
    {
        IInputReader Input { get; }
        
        IGroupsLinesOutput Output { get; }
    }
}
