namespace Bigsort.Lib
{
    public class FileLinesSorter
    {
        public static void Sort(string input, string output) =>
            new IoC().BuildSorter().Sort(input, output);
    }
}
