using System.Collections.Generic;

namespace Bigsort.Contracts
{
    // ReSharper disable once InconsistentNaming
    public interface IGrouperIOMaker
    {
        IGrouperIO Make(string input, string output,
            IPool<byte[]> buffersPool);

        /// <summary>
        /// Creates '<paramref name="count"/>' or less (when create 'count' is not possible) 
        /// <see cref="IGrouperIO"/>s.
        /// </summary>
        IReadOnlyList<IGrouperIO> MakeMany(string input, string output, int count,
            IPool<byte[]> buffersPool);
    }
}
