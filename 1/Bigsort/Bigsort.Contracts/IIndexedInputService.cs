using System.Collections.Generic;

namespace Bigsort.Contracts
{
    internal interface IIndexedInputService
    {
        IIndexedInput MakeInput(
            IReadOnlyList<long> linesStarts,
            IReadingStream input);

        IIndexedInput DecorateForStringsSorting(
            IIndexedInput core,
            IReadOnlyList<int> dotsShifts);

        IIndexedInput DecorateForNumbersSorting(
            IIndexedInput core,
            IReadOnlyList<int> dotsShifts);
    }
}
