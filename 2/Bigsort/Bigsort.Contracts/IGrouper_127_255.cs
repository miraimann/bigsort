namespace Bigsort.Contracts
{
    /// <summary>
    /// <see cref="IGrouper">IGrouper</see> 
    /// with folloving bytes mapping in lines of result:
    /// 
    ///   [-][x][19303][.][hello world]
    ///  
    /// where: 
    ///   last seven bits of [x] contains digits count (max = 127);
    ///   [.] contains letters count (max = 255).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public interface IGrouper_127_255
        : IGrouper
    {
    }
}
