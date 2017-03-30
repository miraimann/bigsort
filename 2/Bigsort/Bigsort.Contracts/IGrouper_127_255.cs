namespace Bigsort.Contracts
{
    /// <summary>
    /// <see cref="IGrouper">IGrouper</see> 
    /// with folloving bytes mapping in lines of result:
    /// 
    ///   [x][y][19303][.][hello world]
    ///  
    /// where: 
    ///   [x] - offset of ordering;
    ///   first bit of [y] - 1 - order by number, 0 - by string;    
    ///   last seven bits of [y] - digits count (max = 127);
    ///   [.] - letters count (max = 255).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public interface IGrouper_127_255
        : IGrouper
    {
    }
}
