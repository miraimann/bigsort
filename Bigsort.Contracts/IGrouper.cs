namespace Bigsort.Contracts
{
    internal interface IGrouper
    {
        GroupInfo[] SeparateInputToGroups();
    }
}
