using System;

namespace Bigsort.Contracts
{
    internal interface ISortersMaker
    {
        ISorter MakeSymbolBySymbolSorter(
            IIndexedInput input,
            Action<int> nextLineFound,
            ISorter subSorter,
            Func<int, int> hashFunc = null,
            int abcLength = byte.MaxValue + 1);

        ISorter MakeLengthSorter(
            IIndexedInput input,
            Action<int> nextLineFound,
            ISorter subSorter);

        ISorter MakeNoSortSorter(
            Action<int> nextLineFound);
    }
}
