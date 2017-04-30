namespace Bigsort.Lib
{
    public class FileLinesSorter
    {
        public static void Sort(string input, string output) =>
            new IoC().BuildBigSorter(input, output).Sort();
    }
}
