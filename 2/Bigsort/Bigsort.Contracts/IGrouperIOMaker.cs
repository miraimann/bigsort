using System.Collections.Generic;

namespace Bigsort.Contracts
{
    // ReSharper disable once InconsistentNaming
    public interface IGrouperIOMaker
    {
        IGrouperIO Make(string input, string output);

        /// <summary>
        /// Creates [<paramref name="count"/>] or less (when create [count] is not possible) 
        /// <see cref="IGrouperIO"/>s.
        /// </summary>
        /// <param name="input">Path of file for grouping.</param>
        /// <param name="output">File path for write grouping result.</param>
        /// <param name="count">
        /// Count of <see cref="IGrouperIO"/>s in result (for multithreading use).
        /// </param>
        /// <returns></returns>
        IReadOnlyList<IGrouperIO> MakeMany(string input, string output, int count);
    }
}
