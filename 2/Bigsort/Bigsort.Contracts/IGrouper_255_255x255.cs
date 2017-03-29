namespace Bigsort.Contracts
{
    /// <summary>
    /// <see cref="IGrouper">IGrouper</see> 
    /// with folloving bytes mapping in lines of result:
    /// 
    ///   [x][y][19303][.][hello world]
    ///  
    /// where: 
    ///   [y] contains digits count (max = 255);
    ///   [x] * 255 + [.] is letters count (max = 255*255).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public interface IGrouper_255_255x255
        : IGrouper
    {
    }
}
