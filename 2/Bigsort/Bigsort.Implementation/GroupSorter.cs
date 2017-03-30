using System;
using Bigsort.Contracts;

namespace Bigsort.Implementation
{
    public class GroupSorter
        : IGroupSorter
    {
        private readonly ISortingFragmentsMoverMaker _sortingFragmentsMoverMaker;
        private readonly ILinesIndexator _linesIndexator;

        public GroupSorter(
            ISortingFragmentsMoverMaker sortingFragmentsMoverMaker,
            ILinesIndexator linesIndexator)
        {
            _sortingFragmentsMoverMaker = sortingFragmentsMoverMaker;
            _linesIndexator = linesIndexator;
        }

        public void Sort(
            IGroup group,
            ArrayFragment<SortingLine> linesFragment)
        {
            var lines = linesFragment.Array;
            var linesComparer = new SortingLine.Comparer();
            var sortingFragmentsMover = _sortingFragmentsMoverMaker
                .Make(group, lines);

            _linesIndexator
                .IndexLines(group, linesFragment);

            sortingFragmentsMover.MoveNext(
                linesFragment.Offset,
                linesFragment.Count);

            Array.Sort(
                lines,
                linesFragment.Offset,
                linesFragment.Count,
                linesComparer);

            int n = linesFragment.Offset + linesFragment.Count - 1,
                offset = linesFragment.Offset,
                next = 0,
                count = 1;

            SortingLine 
                line = default(SortingLine),
                nextLine = default(SortingLine);
            
            while (offset < n)
            {
                var isSingle = count == 1;
                if (isSingle)
                {
                    line = lines[offset];
                    nextLine = lines[next = offset + 1];
                }

                if (nextLine.fragmentForSort == line.fragmentForSort &&
                    nextLine.fragmentForSort != uint.MaxValue)
                    nextLine = lines[next + ++count];
                else if (isSingle) offset++;
                else
                {
                    sortingFragmentsMover.MoveNext(offset, count);
                    Array.Sort(lines, offset, count, linesComparer);
                    count = 1;
                }
            }
        }
    }
}
