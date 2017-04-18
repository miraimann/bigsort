using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bigsort.Tools.SortedFileChecker
{
    public static class Checker
    {
        private static readonly IComparer<char> CharComparer =
            Comparer<char>.Default;

        private static readonly IComparer<int> IntComparer =
            Comparer<int>.Default;

        public static bool IsSorted(string path)
        {
            var prevLine = "0."; // min line
            using (var stream = File.OpenRead(path))
            using (var reader = new StreamReader(stream, Encoding.ASCII))
                while (!reader.EndOfStream)
                {
                    var currLine = reader.ReadLine();
                    var comparationResult = Compare(prevLine, currLine);
                    if (comparationResult > 0)
                        return false;

                    prevLine = currLine;
                }
            
            return true;
        }

        private static int Compare(string x, string y)
        {
            int xDotIndex = x.IndexOf('.'),
                yDotIndex = y.IndexOf('.'),
                comparationResult = 0;

            int i, j;
            for (i = xDotIndex + 1, j = yDotIndex + 1;
                 i < x.Length && j < y.Length;
                 i++, j++)
            {
                comparationResult =
                    CharComparer.Compare(x[i], y[j]);

                if (comparationResult != 0)
                    return comparationResult;
            }

            bool xOver = i >= x.Length,
                 yOver = j >= y.Length;

            if (xOver ^ yOver)
                return xOver ? -1 : 1;
            
            // for (i = 0; i < xDotIndex && x[i] == '0'; i++) ;
            // for (j = 0; j < yDotIndex && y[j] == '0'; j++) ;
            
            comparationResult =
                IntComparer.Compare(xDotIndex - i, yDotIndex - j);

            if (comparationResult != 0)
                return comparationResult;

            for (; i < xDotIndex; i++, j++)
            {
                comparationResult =
                    CharComparer.Compare(x[i], y[j]);

                if (comparationResult != 0)
                    return comparationResult;
            }

            return 0;
        }
    }
}
